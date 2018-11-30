using UnityEngine;
using System.Collections;
using System;
using System.Xml.Linq;
using System.Collections.Specialized;
using System.Linq;

namespace DarkRift.Server.Unity
{

    public class EnetServer : MonoBehaviour
    {
        /// <summary>
        ///     The actual server.
        /// </summary>
        public DarkRiftServer Server { get; private set; }

        [SerializeField]
        [Tooltip("The configuration file to use.")]
        TextAsset configuration;

        [SerializeField]
        [Tooltip("Indicates whether the server will be created in the OnEnable method.")]
        bool createOnEnable = true;

        [SerializeField]
        [Tooltip("Indicates whether the server events will be routed through the dispatcher or just invoked.")]
        bool eventsFromDispatcher = true;

        [SerializeField]
        [Tooltip("Specifies whether the server should receive messages in Update(), FixedUpdate() or manually")]
        UpdateMode updateMode;

        private EnetListenerPlugin enetListener;

        void OnEnable()
        {
            //If createOnEnable is selected create a server
            if (createOnEnable)
                Create();
        }

        void Update()
        {
            if (updateMode == UpdateMode.Update)
            {
                ReceiveMessages();
            }
        }

        void FixedUpdate()
        {
            if (updateMode == UpdateMode.FixedUpdate)
            {
                ReceiveMessages();
            }
        }

        /// <summary>
        ///     Creates the server.
        /// </summary>
        public void Create()
        {
            Create(new NameValueCollection());
        }

        /// <summary>
        ///     Creates the server.
        /// </summary>
        public void Create(NameValueCollection variables)
        {
            if (Server != null)
                throw new InvalidOperationException("The server has already been created! (Is CreateOnEnable enabled?)");
            
            if (configuration != null)
            {
                //Create spawn data from config
                ServerSpawnData spawnData = ServerSpawnData.CreateFromXml(XDocument.Parse(configuration.text), variables);

                //Inaccessible from xml, set from inspector
                spawnData.EventsFromDispatcher = eventsFromDispatcher;

                //Unity is broken, work around it...
                //This is an obsolete property but is still used if the user is using obsolete <server> tag properties
#pragma warning disable 0618
                spawnData.Server.UseFallbackNetworking = true;
#pragma warning restore 0618

                //Add types
                spawnData.PluginSearch.PluginTypes.AddRange(UnityServerHelper.SearchForPlugins());
                spawnData.PluginSearch.PluginTypes.Add(typeof(UnityConsoleWriter));

                //Create server
                Server = new DarkRiftServer(spawnData);
                Server.Start();
                enetListener = Server.NetworkListenerManager.GetNetworkListenersByType<EnetListenerPlugin>().First();
            }
            else
                Debug.LogError("No configuration file specified!");
        }


        /// <summary>
        /// Call this to manually receive messages
        /// </summary>
        public void ReceiveMessages()
        {
            if (Server != null)
            {
                enetListener?.ServerTick();
                Server.ExecuteDispatcherTasks();
            }
        }

        void OnDisable()
        {
            Close();
        }

        void OnApplicationQuit()
        {
            Close();
        }

        /// <summary>
        ///     Closes the server.
        /// </summary>
        public void Close()
        {
            if (Server != null)
                Server.Dispose();
        }
    }
}
