/*
 * Created by SharpDevelop.
 * User: mifus_000
 * Date: 20/05/2017
 * Time: 09:00
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Data;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Collections.Generic;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Runtime.InteropServices;
using System.Globalization;

using System.Threading.Tasks;

class Program
{

    static int totalThread = 0;
    public static string location = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) + "\\";
    public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
    {
        // Unix timestamp is seconds past epoch
        System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
        dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
        return dtDateTime;
    }


    class EntryArbitrage
    {
        public IExchange exchangeBuy;
        public string pairBuy = "btc_brl";
        public string pairSell = "btc_brl";
        public IExchange exchangeSell;
        public decimal perc = 1m;
        public decimal amount = 0.001m;
        public int sleep = 100;
    }


    static readonly Object objLock = new Object();
    public static void arbitrageDetail(Object obj)
    {
        EntryArbitrage entry = (EntryArbitrage)obj;
        lock (objLock)
        {
            entry.exchangeBuy.getBalances();
            entry.exchangeSell.getBalances();
        }
        while (true)
        {
            try
            {

                decimal[] buy = null;
                decimal[] sell = null;
                Task.Run(() =>
                {
                    buy = entry.exchangeBuy.getLowestAsk(entry.pairBuy, entry.amount);
                });
                Task.Run(() =>
                {
                    sell = entry.exchangeSell.getHighestBid(entry.pairSell, entry.amount);
                });
                while (true)
                {
                    if (buy != null && sell != null)
                        break;
                    System.Threading.Thread.Sleep(50);
                }
                decimal perc = (((sell[0] * 100) / buy[0]) - 100);
                Logger.log(Math.Round(perc, 2) + "% " + entry.exchangeBuy.getName() + " > " + entry.exchangeSell.getName() + " ");
                if (perc > entry.perc)
                {

                    if (entry.exchangeBuy.getBalance("USDT") >= (buy[0] * entry.amount) && entry.exchangeSell.getBalance("BTC") >= entry.amount)
                    {
                        Task.Run(() =>
                        {
                            entry.exchangeBuy.order("buy", entry.pairBuy, entry.amount, buy[1], false);
                        });
                        Task.Run(() =>
                        {
                            entry.exchangeSell.order("sell", entry.pairSell, entry.amount, sell[1], false);
                        });

                        System.Threading.Thread.Sleep(2000);
                        lock (objLock)
                        {
                            entry.exchangeBuy.getBalances();
                            entry.exchangeSell.getBalances();
                        }

                        Logger.log(entry.exchangeBuy.getName() + " > " + entry.exchangeSell.getName() + " | Buy[0]" + buy[0] + " Buy[1]" + buy[1] + "| Sell[0]" + sell[0] + " Sell[1]" + sell[1] + " | " + perc + "%");

                        System.Threading.Thread.Sleep(2000);
                    }

                    Logger.log(entry.exchangeBuy.getName() + " > " + entry.exchangeSell.getName() + " | Buy[0]" + buy[0] + " Buy[1]" + buy[1] + "| Sell[0]" + sell[0] + " Sell[1]" + sell[1] + " | " + perc + "%");

                }
            }
            catch
            {

            }
            System.Threading.Thread.Sleep(500);
            System.Threading.Thread.Sleep(entry.sleep);
        }
    }
    public static void arbitrageHFT()
    {



        try
        {

            EntryArbitrage entryBraziliexBitCointrade = new EntryArbitrage();
            entryBraziliexBitCointrade.exchangeBuy = new ExchangeBraziliex();
            entryBraziliexBitCointrade.pairBuy = "btc_brl";
            entryBraziliexBitCointrade.exchangeSell = new ExchangeBitcoinTrade();
            entryBraziliexBitCointrade.pairSell = "BTC";
            entryBraziliexBitCointrade.sleep = int.Parse(Program.jConfig["sleep_default"].ToString()); ;
            entryBraziliexBitCointrade.perc = decimal.Parse(Program.jConfig["arbitrage_percent"].ToString()); ;
            entryBraziliexBitCointrade.amount = decimal.Parse(Program.jConfig["arbitrage_amount"].ToString());
            System.Threading.Thread tBraziliexBitCointrade = new System.Threading.Thread(arbitrageDetail);
            tBraziliexBitCointrade.Start(entryBraziliexBitCointrade);


            EntryArbitrage entryBitCointradeBraziliex = new EntryArbitrage();
            entryBitCointradeBraziliex.exchangeBuy = new ExchangeBitcoinTrade();
            entryBitCointradeBraziliex.pairBuy = "BTC";
            entryBitCointradeBraziliex.exchangeSell = new ExchangeBraziliex();
            entryBitCointradeBraziliex.pairSell = "btc_brl";
            entryBitCointradeBraziliex.sleep = int.Parse(Program.jConfig["sleep_default"].ToString());
            entryBitCointradeBraziliex.perc = decimal.Parse(Program.jConfig["arbitrage_percent"].ToString()); ;
            entryBitCointradeBraziliex.amount = decimal.Parse(Program.jConfig["arbitrage_amount"].ToString());
            System.Threading.Thread tBitCointradeBraziliex = new System.Threading.Thread(arbitrageDetail);
            tBitCointradeBraziliex.Start(entryBitCointradeBraziliex);


        }
        catch
        {

        }

        while (true)
        {
            System.Threading.Thread.Sleep(600000);
        }
    }




    public static JContainer jConfig = null;
    public static void Main(string[] args)
    {

        try
        {

            String jsonConfig = System.IO.File.ReadAllText(location + "key.txt");
            jConfig = (JContainer)JsonConvert.DeserializeObject(jsonConfig, (typeof(JContainer)));

            arbitrageHFT();

        }
        catch (Exception ex)
        {

            return;
        }

    }

}
