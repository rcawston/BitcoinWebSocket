using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using BitcoinWebSocket.Schema;
using BitcoinWebSocket.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BitcoinWebSocket.Bitcoin
{
    /// <summary>
    ///     Handles Bitcoin RPC Requests
    /// </summary>
    public class RPCClient
    {
        public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(45);
        private readonly string _username;
        private readonly string _password;
        private readonly Uri _connectTo;

        /// <summary>
        ///     Contructor
        ///     - saves connection and auth data
        /// </summary>
        /// <param name="connectTo">Bitcoin RPC URI</param>
        /// <param name="username">RPC Username</param>
        /// <param name="password">RPC Password</param>
        public RPCClient(Uri connectTo, string username, string password)
        {
            _connectTo = connectTo;
            _username = username;
            _password = password;
        }

        /// <summary>
        ///     Gets a list of all chain tips
        /// </summary>
        /// <returns>array of ChainTips</returns>
        public ChainTip[] GetChainTips()
        {
            var ret = SendCommand("getchaintips");
            var result = ret.GetValue("result").ToObject<ChainTip[]>();
            return result;
        }

        /// <summary>
        ///     Gets the branch length of a given chain tip hash
        /// </summary>
        /// <returns>length of the branch, or -1 if not found</returns>
        public int GetBranchLength(string blockHash)
        {
            var chainTips = GetChainTips();
            var chainTip = chainTips.First(x => string.Equals(x.hash, blockHash, StringComparison.CurrentCultureIgnoreCase));
            if (chainTip == null)
                return -1;
            return chainTip.branchlen;
        }

        /// <summary>
        ///     Gets the current block height
        /// </summary>
        /// <returns>block height</returns>
        public int GetBlockCount()
        {
            var ret = SendCommand("getblockcount");
            var result = ret.GetValue("result");
            return result.Value<int>();
        } 
        
        /// <summary>
        ///     Gets the block hash at a given height
        /// </summary>
        /// <returns>block hash in hex</returns>
        public string GetBlockHash(int height)
        {
            var ret = SendCommand("getblockhash", height);
            var result = ret.GetValue("result");
            return result.Value<string>();
        }

        /// <summary>
        ///     Gets the height of a given block
        /// </summary>
        /// <returns>raw block data as hex</returns>
        public string GetBlockData(string blockHash)
        {
            var ret = SendCommand("getblock", blockHash, 0);
            var result = ret.GetValue("result");
            return result.Value<string>();
        }

        /// <summary>
        ///     Gets the height of a given block
        /// </summary>
        /// <returns>block height</returns>
        public int GetBlockHeight(string blockHash)
        {
            var ret = SendCommand("getblock", blockHash);
            var result = ret.GetValue("result");
            return result.Value<int>("height");
        }

        /// <summary>
        ///     Gets raw transaction data for a given txid
        /// </summary>
        /// <param name="txid">transaction id in hex</param>
        /// <returns>raw transaction byte data</returns>
        public byte[] GetRawTransaction(string txid)
        {
            var ret = SendCommand("getrawtransaction", txid);
            var transactionHex = ret.GetValue("result").ToObject<string>();
            return ByteToHex.StringToByteArray(transactionHex);
        }

        /// <summary>
        ///     Gets all transaction ids in the mem pool
        /// </summary>
        /// <returns>array of txid in hex</returns>
        public string[] GetMemPool()
        {
            var ret = SendCommand("getrawmempool");
            return ret.GetValue("result").ToObject<string[]>();
        }

        /// <summary>
        ///     Sends a command to the JSON/RPC interface
        /// </summary>
        /// <param name="method">method to call</param>
        /// <param name="parameters">parameters to pass</param>
        /// <returns></returns>
        private JObject SendCommand(string method, params object[] parameters)
        {
            // create HttpWebRequest
            var address = _connectTo.AbsoluteUri;

            var webRequest = (HttpWebRequest)WebRequest.Create(address);
            webRequest.Credentials = new NetworkCredential(_username, _password);
            //webRequest.Headers[HttpRequestHeader.Authorization] = "Basic " + EncodeToBase64(_authentication);
            webRequest.ContentType = "application/json-rpc";
            webRequest.Method = "POST";
            webRequest.KeepAlive = false;
            webRequest.Timeout = (int) RequestTimeout.TotalMilliseconds;

            var requestJson = new JObject
            {
                ["jsonrpc"] = "1.0",
                ["id"] = "1",
                ["method"] = method
            };
            if (parameters != null && parameters.Length > 0)
            {
                var props = new JArray();
                foreach (var p in parameters)
                    props.Add(p);
                requestJson.Add(new JProperty("params", props));
            }
            var request = JsonConvert.SerializeObject(requestJson);
            // serialize json for the request
            var byteData = Encoding.UTF8.GetBytes(request);
            webRequest.ContentLength = byteData.Length;

            // may throw WebException
            using (var dataStream = webRequest.GetRequestStream())
                dataStream.Write(byteData, 0, byteData.Length);

            try
            {
                HttpWebResponse webResponse;
                using (webResponse = (HttpWebResponse) webRequest.GetResponse())
                {
                    using (var str = webResponse.GetResponseStream())
                    {
                        // TODO: str could be null
                        using (var sr = new StreamReader(str))
                        {
                            return JsonConvert.DeserializeObject<JObject>(sr.ReadToEnd());
                        }
                    }
                }
            }
            catch (WebException e)
            {
                using (var str = e.Response.GetResponseStream())
                {
                    using (var sr = new StreamReader(str))
                    {
                        var tempRet = JsonConvert.DeserializeObject<JObject>(sr.ReadToEnd());
                        return tempRet;
                    }
                }
            }
        }

        /// <summary>
        ///     Converts a string to Base64 encoded string
        /// </summary>
        /// <param name="toEncode">string to encode</param>
        /// <returns>base64 string</returns>
        private static string EncodeToBase64(string toEncode)
        {
            return Convert.ToBase64String(Encoding.ASCII.GetBytes(toEncode));
        }
    }
}