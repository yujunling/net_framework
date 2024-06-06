using System;
using System.Net;
using System.Net.Security;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using BestHTTP;
using com.yoyo.avatar.protocol;
using Core.Interface;
using Core.Net;
using Plugins.LiveAvatarSDK.include.AppDeviceInfo;
using Plugins.LiveAvatarSDK.include.PBBridge;
using UnityEngine;
using System.Collections.Generic;

namespace Managers.Net
{
    public delegate void SendMsgDelegate(Protocol protocol, Action<int, string> errorHandler = null);
    public class NetManager
    {
        private static readonly NetManager Instance=new NetManager();
        private NetManager(){}

        public static NetManager GetInstance()
        {
            return Instance;
        }
        ///////////////////////////////////////////////////
        //event
        public event Action<AppSocket> SocketConnectedEvent;
        public event Action<AppSocket> SocketDisConnectEvent;
        ///
        private AppSocket _mainSocket;
        private IProtocolDecoder _protocolDecoder;
        private IProtocolEncoder _protocolEncoder;
        private SendMsgDelegate _sendFunc;
        private NetCallbackFactory _callbackFactory;
        public void Init()
        {
            _deviceId= GameUtils.getDeviceId();
            _version = AppDeviceInfo.GetVersion();
            _callbackFactory=new NetCallbackFactory();
            _sendFunc = (p,f) => { Debug.LogError("connect has not connected");};
            
            _httpUri=new Uri(NetConfig.GetHttpUrl());
//            _protocolDecoder=new NetProtocolDecoder();
//            _protocolEncoder=new NetProtocolEncoder();
//            _mainSocket= new AppSocket("GameSocket", new NetDataReceiver());
//            _mainSocket.ConnectedEvent += OnConnected;
//            _mainSocket.Connect("ali-test1.ss3w.com",31188);
            ServicePointManager.ServerCertificateValidationCallback = CertificateValidationCallback;
        }
        
        private bool CertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None) return true;
            foreach (var status in chain.ChainStatus)
            {
                if (status.Status == X509ChainStatusFlags.RevocationStatusUnknown)
                {
                    continue;
                }
                chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
                chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
                chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan(0, 1, 0);
                chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllFlags;
                var chainIsValid = chain.Build((X509Certificate2)certificate);
                if (chainIsValid) continue;
                return false;
            }
            return true;
        }

        public bool IsNetworkAvailable()
        {
            return Application.internetReachability!= NetworkReachability.NotReachable;
        }

        public void InitSocket(uint uid,string session,string key)
        {
            Debug.Log($"init socket:id:{uid}  session:{session}  key:{key}");
            NetDog.Instance.init();
            NetConfig.GetHostAndIP(out var ip, out var port);
            NetDog.Instance.ConnectServer_World(ip, port, uid, session,key);
        }

        public void RegisterCallback(int code,Action<Protocol> handler)
        {
            _callbackFactory.RegisterHandler(code,handler);
        }

        public void UnRegisterCallback(int code, Action<Protocol> handler)
        {
            _callbackFactory.UnRegisterHandler(code,handler);
        }
        
        private void OnConnected(AppSocket obj)
        {
            Debug.Log($"socket {obj.Name} connected");
            _sendFunc = SendMsgFunc;
            SocketConnectedEvent?.Invoke(obj);
        }
        //////////////////////////////////////////////////////////////////
        [Obsolete("接管老的socket连接，后面可以去掉")]
        public void SocketConnected()
        {
            SocketConnectedEvent?.Invoke(_mainSocket);
        }
        /// 
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <param name="port"></param>

        public void ConnectMainSocket(string url, int port)
        {
            if (_mainSocket.IsConnected)
            {
                Debug.LogError("main socket has connected");
                return;
            }
            _mainSocket.Connect(url,port);
        }

        private void SendMsgFunc(Protocol protocol, Action<int, string> errorHandler = null)
        {
            _mainSocket.SendMessage(_protocolEncoder.Encode(protocol)); 
        }
        /// <summary>
        /// 向主socket发送消息
        /// </summary>
        /// <param name="protocol"></param>
        /// <param name="errorHandler"></param>
        private void SendMsg(Protocol protocol, Action<int, string> errorHandler=null)
        {
            _sendFunc(protocol, errorHandler);
        }
        /////////////////////////////////////////////////////////////////////////////////////
        private Uri _httpUri;
        private string _deviceId;
        private string _version;
        public void SendPostRequest(Protocol protocol)
        {
            if (protocol == null) return;
            var objType = protocol.GetType();
            protocol.msg_id(CSharpPBNativeInterface.getMsgID());
            if (protocol.uid() <= 0)
            {
                //设置uid
                protocol.uid(UserDataManager.Instance.Uid_Int);
            }
            //设置checksum
            Type valueType = typeof(CheckSumInfo);
            MethodInfo method = objType.GetMethod("checksum", new Type[] { valueType });
            if (method != null)
            {
                object[] _params = { CSharpPBNativeInterface.getDefaultCheckSum(protocol, (int)protocol._uid, _deviceId, _version) };
                method.Invoke(protocol, _params);
            }

            var request = new HTTPRequest(_httpUri, HTTPMethods.Post, PostRequestComplete)
            {
                Timeout = TimeSpan.FromSeconds(15),
                ConnectTimeout = TimeSpan.FromSeconds(15),
                RawData = CSharpPBNativeInterface.convertProtocol2Bytes(protocol)
            };
            request.Send();
        }

        private void PostRequestComplete(HTTPRequest request, HTTPResponse response)
        {
            Debug.LOGD("request complete");
            
            if (request.State == HTTPRequestStates.Finished && response.IsSuccess) 
            {
                var callbackProtocol = CSharpPBNativeInterface.ConvertBytes2Protocol(response.Data);
                if (callbackProtocol == null)
                {
                    Debug.LogError("can not parse net post callback message");
                    return;
                }
                CSharpPBNativeInterface.printfProtocolDebugString(callbackProtocol);
                HandleCallbackProtocol(callbackProtocol);
            }
        }

        /// <summary>
        /// 处理一条回调协议
        /// </summary>
        /// <param name="protocol"></param>
        public void HandleCallbackProtocol(Protocol protocol)
        {
            var code = protocol._uri;
            var handler = _callbackFactory.GetOneHandler(code);
            if (handler == null)
            {
                Debug.LogWarning($"can not find call back handler,protocol code: {code}");
                return;
            }          
            handler.Invoke(protocol);
            NetWorkHelper.onRecvProtocol(protocol.GetType());
        }

        public void CloseServer()
        {
            SocketDisConnectEvent?.Invoke(null);
        }
    }
}