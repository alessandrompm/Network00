using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.NetworkInformation; //questo namespace contiene algritmi di utilità per l'analisi della rete
using System.Net;                    //strutture e oggetti comuni 
using System.Threading;

namespace Network00
{
    //IP Scanner
    class Program
    {
        static CountdownEvent hCountDown;
        //Esercizio
        //Dati 2 indirizzi scansionare nel minor tempo possibile il range di IP validi
        static void Main(string[] args)
        {
            //if(args.Length != 2)
            //{
            //    Console.WriteLine("Usage: Scan <indirizzoiniziale v4>  <indirizzofinale v4>");
            //    return;
            //}

            args = new string[] { "192.168.1.0", "192.168.1.255" };

           
            //string => ipaddress => bytes => int
            uint iStart = BitConverter.ToUInt32(IPAddress.Parse(args[0]).GetAddressBytes().Reverse().ToArray(), 0);
            uint iEnd   = BitConverter.ToUInt32(IPAddress.Parse(args[1]).GetAddressBytes().Reverse().ToArray(), 0);


            //Generiamo l'insieme di indirizzi scansionabili
            IEnumerable<IPAddress> hRange = Enumerable.Range((int)iStart, (int)(iEnd - iStart + 1)).Select(i => BitConverter.GetBytes(i).Reverse().Select(b => b.ToString()).Aggregate((x, y) => x + "." + y)).Select(addr => IPAddress.Parse(addr));

            #region Implementazione con i Task

            //TaskFactory hFactory    = new TaskFactory();
            //List<Task>  hTasks      = new List<Task>();

            //foreach (IPAddress hAddr in hRange)
            //{
            //    hTasks.Add(hFactory.StartNew(() =>
            //    {
            //        using (Ping hPing = new Ping())
            //        {
            //            PingReply hReply = hPing.Send(hAddr);

            //            lock(hSyncRoot)
            //                Console.WriteLine($"{hAddr} => {hReply.Status} {hReply.RoundtripTime}ms");
            //        }
            //    }));                
            //}

            //Task.WaitAll(hTasks.ToArray());

            #endregion


            #region SendAsync

            hCountDown = new CountdownEvent(hRange.Count());

            //Poesia
            foreach (IPAddress hAddr in hRange)
            {
                Ping hPing = new Ping();

                hPing.PingCompleted += (sender, e) =>
                {
                    Ping hOriginal = sender as Ping;

                    if (e.Reply.Status == IPStatus.Success)
                    {
                        Console.WriteLine($"{e.Reply.Address} => {e.Reply.Status} {e.Reply.RoundtripTime}ms");
                    }

                    hCountDown.Signal();
                    hOriginal.Dispose();
                };

                hPing.SendAsync(hAddr, 3000, hPing);
            }

            hCountDown.Wait();


            #endregion

            Console.WriteLine("Scan Completed!");
            Thread.CurrentThread.Join();
            
        }




    }
}
