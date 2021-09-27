using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

public class ExchangeBitcoinTrade : ExchangeBase, IExchange
{
    public static decimal balance_usdt = 0;
    public static decimal balance_btc = 0;

    public ExchangeBitcoinTrade()
    {
        this.urlTicker = "https://bitcointrade.com.br";
        this.key = Program.jConfig["bitcointrade_key"].ToString();
        this.secret = Program.jConfig["bitcointrade_secret"].ToString();
        this.lockQuantity = false;
        this.fee = decimal.Parse( Program.jConfig["bitcointrade_fee"].ToString());
    }

    public decimal getFee()
    {
        return this.fee;
    }


    public string getName()
    {
        return "BITCOINTRADE";

    }


    public decimal calculateAmount(decimal amount, string pair)
    {
        return amount;
    }


    public string getBalances()
    {

        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        var client = new RestClient("https://api.bitcointrade.com.br/v1/");


        var request = new RestRequest("/wallets/balance", Method.GET);
        request.AddHeader("Authorization", "ApiToken " + this.getSecret());
        var response = client.Execute(request);

        String json = response.Content.ToString();
        Logger.log(json);

        JContainer jContainer = (JContainer)JsonConvert.DeserializeObject(json, (typeof(JContainer)));

        balance_btc = decimal.Parse(jContainer["data"][1]["available_amount"].ToString().Replace(".", ","));
        balance_usdt = decimal.Parse(jContainer["data"][0]["available_amount"].ToString().Replace(".", ","));

        return json;
    }

    public decimal getLastValue(string pair)
    {

        try
        {
            String json = Http.get("https://api.bitcointrade.com.br/v1/public/BTC/ticker");

            JContainer j = (JContainer)JsonConvert.DeserializeObject(json, (typeof(JContainer)));

            return decimal.Parse(j["data"]["last"].ToString().Replace(".", ","), System.Globalization.NumberStyles.Float);
        }
        catch
        {
        }
        return 0;

    }

    public decimal[] getLowestAsk(string pair, decimal amount)
    {


        try
        {

            String json = Http.get("https://api.bitcointrade.com.br/v1/public/BTC/orders");
            JContainer jCointaner = (JContainer)JsonConvert.DeserializeObject(json, (typeof(JContainer)));


            decimal[] arrayValue = new decimal[2];
            arrayValue[0] = arrayValue[1] = 0;
            decimal amountBook = 0;
            decimal amountAux = 0;
            decimal total = 0;
            int lines = 0;

            foreach (var item in jCointaner["data"]["asks"])
            {
                lines++;
                amountBook += decimal.Parse(item["amount"].ToString().Replace(".", ","));

                if (amount > amountBook)
                {
                    total += decimal.Parse(item["amount"].ToString().Replace(".", ",")) * decimal.Parse(item["unit_price"].ToString().Replace(".", ","));
                    amountAux += decimal.Parse(item["amount"].ToString().Replace(".", ","));
                }
                else if (lines == 1)
                {
                    arrayValue[0] = decimal.Parse(item["unit_price"].ToString().Replace(".", ","));
                    arrayValue[1] = decimal.Parse(item["unit_price"].ToString().Replace(".", ","));
                    return arrayValue;
                }
                else
                    total += (amount - amountAux) * decimal.Parse(item["unit_price"].ToString().Replace(".", ","));

                if (amountBook >= amount)
                {
                    arrayValue[0] = (total / amount) ;
                    arrayValue[1] = decimal.Parse(item["unit_price"].ToString().Replace(".", ","));
                    return arrayValue;
                }
            }
        }
        catch
        {
        }
        return new decimal[2];
    }

    public decimal[] getHighestBid(string pair, decimal amount)
    {
        try
        {

            String json = Http.get("https://api.bitcointrade.com.br/v1/public/BTC/orders");
            JContainer jCointaner = (JContainer)JsonConvert.DeserializeObject(json, (typeof(JContainer)));


            decimal[] arrayValue = new decimal[2];
            arrayValue[0] = arrayValue[1] = 0;
            decimal amountBook = 0;
            decimal amountAux = 0;
            decimal total = 0;
            int lines = 0;

            foreach (var item in jCointaner["data"]["bids"])
            {
                lines++;
                amountBook += decimal.Parse(item["amount"].ToString().Replace(".", ","));

                if (amount > amountBook)
                {
                    total += decimal.Parse(item["amount"].ToString().Replace(".", ",")) * decimal.Parse(item["unit_price"].ToString().Replace(".", ","));
                    amountAux += decimal.Parse(item["amount"].ToString().Replace(".", ","));
                }
                else if (lines == 1)
                {
                    arrayValue[0] = decimal.Parse(item["unit_price"].ToString().Replace(".", ","));
                    arrayValue[1] = decimal.Parse(item["unit_price"].ToString().Replace(".", ","));
                    return arrayValue;
                }
                else
                    total += (amount - amountAux) * decimal.Parse(item["unit_price"].ToString().Replace(".", ","));

                if (amountBook >= amount)
                {
                    arrayValue[0] = (total / amount) ;
                    arrayValue[1] = decimal.Parse(item["unit_price"].ToString().Replace(".", ","));
                    return arrayValue;
                }
            }
        }
        catch
        {
        }
        return new decimal[2];
    }


    public void getMarket()
    {
        String json = Http.get(this.urlTicker);
        this.dataSource = (Newtonsoft.Json.Linq.JContainer)JsonConvert.DeserializeObject(json);
    }

    public Operation order(string type, string pair, decimal amount, decimal price, bool lockQuantity)
    {
        Task.Factory.StartNew(() =>
        {
            price = price;
            price = Math.Round(price, 2);

            Operation operation = new Operation();

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            var request = (HttpWebRequest)WebRequest.Create("https://api.bitcointrade.com.br/v1/" + "market/create_order");



            String parameters = "{\"currency\":\"BTC\",\"amount\": " + amount.ToString().Replace(",", ".") + ",\"type\": \"" + type + "\",\"subtype\": \"limited\",\"unit_price\": " + Convert.ToString(price).Replace(",", ".") + "}";
            var data = Encoding.ASCII.GetBytes(parameters);
            request.Headers["Authorization"] = "ApiToken " + this.getSecret();
            request.Method = "POST";
            request.ContentType = "application/json";
            request.ContentLength = data.Length;

            using (var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }

            var response = (HttpWebResponse)request.GetResponse();
            String result = new StreamReader(response.GetResponseStream()).ReadToEnd();

            operation.json = result;
        });
        return null;
    }

    public string getKey()
    {
        return this.key;
    }

    public string getSecret()
    {
        return this.secret;
    }



    public string post(String url, String parameters, String key, String secret)
    {
        try
        {
            // lock (objLock)
            {
                Logger.log(url + parameters);
                var request = (HttpWebRequest)WebRequest.Create(url);
                //System.Threading.Thread.Sleep(1000);
                parameters = "nonce=" + (decimal.Parse(DateTime.Now.ToString("yyyyMMddHHmmssfffff"))) + "&" + parameters;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                var data = Encoding.ASCII.GetBytes(parameters);

                HMACSHA512 encryptor = new HMACSHA512();
                encryptor.Key = Encoding.ASCII.GetBytes(secret);
                String sign = Utils.ByteToString(encryptor.ComputeHash(data));

                request.Headers["Key"] = key;
                request.Headers["Sign"] = sign;
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = data.Length;

                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }

                var response = (HttpWebResponse)request.GetResponse();
                String result = new StreamReader(response.GetResponseStream()).ReadToEnd();

                Logger.log(result);
                return result;
            }
        }
        catch (Exception ex)
        {
            Logger.log("ERROR POST " + ex.Message + ex.StackTrace);
            return null;
        }
        finally
        {
        }
    }

    public decimal getBalance(string pair)
    {
        if (pair.ToUpper().Trim().IndexOf("BTC") >= 0)
            return balance_btc;
        if (pair.ToUpper().Trim().IndexOf("USDT") >= 0)
            return balance_usdt;

        return this.getBalance(this.getName(), pair);
    }

    public void loadBalances()
    {
        String json = post("https://poloniex.com/tradingApi", "command=returnBalances", this.getKey(), this.getSecret());
        DataTable ds = (DataTable)JsonConvert.DeserializeObject("[" + json + "]", (typeof(DataTable)));
        this.dsBalances = new DataSet();
        this.dsBalances.Tables.Add(ds);
    }

    public OrderStatus getOrder(string idOrder)
    {
        throw new NotImplementedException();
    }

    public bool isLockQuantity()
    {
        return this.lockQuantity;
    }
}
