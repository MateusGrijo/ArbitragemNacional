using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

public class ExchangeBraziliex : ExchangeBase, IExchange
{
    public static decimal balance_usdt = 0;
    public static decimal balance_btc = 0;

    public ExchangeBraziliex()
    {
        this.urlTicker = "https://braziliex.com.br/";
        this.key = Program.jConfig["braziliex_key"].ToString();
        this.secret = Program.jConfig["braziliex_secret"].ToString();
        this.lockQuantity = false;
        this.fee = decimal.Parse(Program.jConfig["bitcointrade_fee"].ToString());
    }

    public decimal getFee()
    {
        return this.fee;
    }


    public string getName()
    {
        return "BRAZILIEX";

    }


    public decimal calculateAmount(decimal amount, string pair)
    {
        return amount;
    }


    public string getBalances()
    {
        String json = post("https://braziliex.com/api/v1/private", "command=balance", this.key, this.secret);
        JContainer jContainer = (JContainer)JsonConvert.DeserializeObject( json , (typeof(JContainer)));

        balance_btc = decimal.Parse(jContainer["balance"]["btc"].ToString().Replace(".",","));
        balance_usdt = decimal.Parse(jContainer["balance"]["brl"].ToString().Replace(".", ","));

        return json;
    }

    public decimal getLastValue(string pair)
    {

        try
        {
            String json = Http.get("https://braziliex.com/api/v1/public/ticker/btc_brl");

            JContainer j = (JContainer)JsonConvert.DeserializeObject(json, (typeof(JContainer)));

            return decimal.Parse(j["last"].ToString().Replace(".", ","), System.Globalization.NumberStyles.Float);
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
            pair = pair.Replace("-", "_").Replace("XLM", "STR").Replace("BCC", "BCH").Replace("USD_XRP", "USDT_XRP").Replace("USDT", "BRL");

            String json = Http.get("https://braziliex.com/api/v1/public/orderbook/" + pair);
            JContainer jCointaner = (JContainer)JsonConvert.DeserializeObject(json, (typeof(JContainer)));

            decimal[] arrayValue = new decimal[2];
            arrayValue[0] = arrayValue[1] = 0;
            decimal amountBook = 0;
            decimal amountAux = 0;
            decimal total = 0;
            int lines = 0;

            foreach (var item in jCointaner["asks"])
            {
                lines++;
                amountBook += decimal.Parse(item["amount"].ToString().Replace(".", ","));

                if (amount > amountBook)
                {
                    total += decimal.Parse(item["price"].ToString().Replace(".", ",")) * decimal.Parse(item["amount"].ToString().Replace(".", ","));
                    amountAux += decimal.Parse(item["amount"].ToString().Replace(".", ","));
                }
                else if (lines == 1)
                {
                    arrayValue[0] = decimal.Parse(item["price"].ToString().Replace(".", ","));
                    arrayValue[1] = decimal.Parse(item["price"].ToString().Replace(".", ","));
                    return arrayValue;
                }
                else
                    total += (amount - amountAux) * decimal.Parse(item["price"].ToString().Replace(".", ","));

                if (amountBook >= amount)
                {
                    arrayValue[0] = total / amount;
                    arrayValue[1] = decimal.Parse(item["price"].ToString().Replace(".", ","));
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
            pair = pair.Replace("-", "_").Replace("XLM", "STR").Replace("BCC", "BCH").Replace("USD_XRP", "USDT_XRP").Replace("USDT", "BRL");

            String json = Http.get("https://braziliex.com/api/v1/public/orderbook/" + pair);
            JContainer jCointaner = (JContainer)JsonConvert.DeserializeObject(json, (typeof(JContainer)));

            decimal[] arrayValue = new decimal[2];
            arrayValue[0] = arrayValue[1] = 0;
            decimal amountBook = 0;
            decimal amountAux = 0;
            int lines = 0;
            decimal total = 0;

            foreach (var item in jCointaner["bids"])
            {
                lines++;
                amountBook += decimal.Parse(item["amount"].ToString().Replace(".", ","));

                if (amount > amountBook)
                {
                    total += decimal.Parse(item["price"].ToString().Replace(".", ",")) * decimal.Parse(item["amount"].ToString().Replace(".", ","));
                    amountAux += decimal.Parse(item["amount"].ToString().Replace(".", ","));
                }
                else if (lines == 1)
                {
                    arrayValue[0] = decimal.Parse(item["price"].ToString().Replace(".", ","));
                    arrayValue[1] = decimal.Parse(item["price"].ToString().Replace(".", ","));
                    return arrayValue;
                }
                else
                    total += (amount - amountAux) * decimal.Parse(item["price"].ToString().Replace(".", ","));

                if (amountBook >= amount)
                {
                    arrayValue[0] = total / amount;
                    arrayValue[1] = decimal.Parse(item["price"].ToString().Replace(".", ","));
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

    public  Operation  order(string type, string pair, decimal amount, decimal price, bool lockQuantity)
    {
        Task.Factory.StartNew(() =>
        {
            amount = this.fixAmount(amount);
            pair = pair.Replace("-", "_").Replace("XLM", "STR").Replace("BCC", "BCH").Replace("USD_XRP", "USDT_XRP");

            String amountAsString = "";
            if (lockQuantity)
            {
                amount = amount / getQuantity(pair);
                amountAsString = Math.Round(amount).ToString().Split(',')[0];
                amountAsString = Convert.ToString(decimal.Parse(amountAsString) * getQuantity(pair));
            }
            else
                amountAsString = amount.ToString();


            Operation operation = new Operation();
            operation.success = false;
            if (amountAsString.Length > 10)
                amountAsString = amountAsString.Substring(0, 10);


            operation.amount = amountAsString;

            String json = post("https://braziliex.com/api/v1/private", "command=" + type + "&market=" + pair + "&amount=" + amountAsString.ToString().Replace(",", ".") + "&price=" + price.ToString().Replace(",", "."), this.key, this.secret);
            operation.json = json;

            
            if (json.Trim().ToLower().IndexOf("error") >= 0 || json.IndexOf("Not enough") >= 0 )
            {
                operation.success = false;
                Logger.log("Problemas! " + json);
            }
            else
            {
                operation.success = true;
            }
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
                String sign = Utils.ByteToString(encryptor.ComputeHash(data)).ToLower();

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
        if (pair == "USDT")
            return balance_usdt;
        if (pair == "BTC")
            return balance_btc;

        return 0m;
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
