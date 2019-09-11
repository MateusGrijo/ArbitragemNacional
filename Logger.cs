/*
 * Created by SharpDevelop.
 * User: mifus_000
 * Date: 20/05/2017
 * Time: 15:46
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.IO;

	/// <summary>
	/// Description of Logger.
	/// </summary>
	public class Logger
	{
		public Logger()
		{
		}
        public static void log(string value,string prefix)
        {
            log(value);
        }


        static Object objLock = new Object();
        public static void log(string value)
		{
            value = "[" + DateTime.Now.ToString() + "]  " + value;

            lock (objLock)
            {
                Console.WriteLine(value);

                try
                {
                    
                        System.IO.StreamWriter w = new StreamWriter(Program.location + DateTime.Now.ToString("yyyyMMdd") + "_logger.txt", true);
                        w.WriteLine(value);
                        w.Close();
                        w.Dispose();
                        w = null;
                    
                }
                catch
                { }
            }
		}
	}
