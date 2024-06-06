namespace Managers.Net
{
    public static class NetConfig
    {
        private static NetConfigData _currentConfigData;

        static NetConfig()
        {
            _currentConfigData=new NetConfigData();
#if USE_TEST_SRV
            _currentConfigData.Host = "soundmantest.avatalk.cn";
            _currentConfigData.DownloadUrl="http://filetest.avatalk.cn/";
            _currentConfigData.WebviewUrl = "http://wwwtest.avatalk.cn/";
#else
            _currentConfigData.Host = "soundman.avatalk.cn";
            _currentConfigData.DownloadUrl = "http://file.avatalk.cn/";
            _currentConfigData.WebviewUrl = "http://www.avatalk.cn/";
#endif
            _currentConfigData.Port = 31188;
            
            _currentConfigData.Update();
        }
        
        public static void GetHostAndIP(out string host,out int port)
        {
            host = _currentConfigData.Host;
            port = _currentConfigData.Port;
        }

        public static string GetDownloadUrl(string relativePath)
        {
            return _currentConfigData.DownloadUrl + relativePath;
        }
        
        public static string GetTemplateUrl(int templateIndex)
        {
            return string.Format(_currentConfigData.TemplateUrl,templateIndex);
        }

        public static string GetHttpUrl()
        {
            return _currentConfigData.HttpServer;
        }

        public static string GetAgreementUrl()
        {
            return _currentConfigData.WebviewUrl + "pages/service.html";
        }

        public static string GetPrivacyUrl()
        {
            return _currentConfigData.WebviewUrl + "pages/policy.html";
        }

        public static string GetWebviewUrl()
        {
            return _currentConfigData.WebviewUrl;
        }
    }

    public class NetConfigData
    {
        public string HttpServer = "";
        public string Host = "soundmantest.avatalk.cn";
        public int Port = 31188;
        public string DownloadUrl =   "http://filetest.avatalk.cn/";
        public string TemplateUrl = "u/20190820/{0}.dat";
        public string WebviewUrl = ""; //web页面url(app内用webview开启的网页: 分享，条款，玩法说明)

        public NetConfigData()
        {
            Update();
        }
        public void Update()
        {
            HttpServer=Host+":31152/t";
        }
    }
}
