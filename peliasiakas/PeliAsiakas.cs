using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

class PeliAsiakas
{
    public static (string, string) peliAsiakkaanViesti(string tila, string palvelimenViesti, int arvaus)
    {
        string uusiTila = tila;
        string viesti = "";

        string[] osat = string.IsNullOrEmpty(palvelimenViesti) ? new string[0] : palvelimenViesti.Split(' ');
        string komento = osat.Length > 0 ? osat[0] : "";

        switch (tila)
        {
            case "CLOSED":
                uusiTila = "JOIN";
                viesti = "JOIN Pelaaja";
                break;

            case "JOIN":
                if (komento == "ACK" && osat.Length >= 2)
                {
                    if (osat[1] == "201")
                    {
                        uusiTila = "JOIN";
                    }
                    else if (osat[1] == "202")
                    {
                        uusiTila = "GAME";
                        viesti = $"DATA {arvaus}";
                    }
                    else if (osat[1] == "203")
                    {
                        uusiTila = "GAME";
                    }
                }
                break;

            case "GAME":
                if (komento == "DATA")
                {
                    // Vastustaja arvasi väärin: kuitataan, ja seuraavaksi on oma vuoro
                    uusiTila = "TURN";
                    viesti = "ACK 300";
                }
                else if (komento == "ACK" && osat.Length >= 2 && osat[1] == "300")
                {
                    uusiTila = "GAME";
                }
                else if (komento == "QUIT")
                {
                    uusiTila = "CLOSED";
                    viesti = "ACK 500";
                }
                else if (komento == "ACK" && osat.Length >= 2 && osat[1] == "407")
                {
                    uusiTila = "GAME";
                    viesti = $"DATA {arvaus}";
                }
                else if (string.IsNullOrEmpty(palvelimenViesti))
                {
                    uusiTila = "GAME";
                    viesti = $"DATA {arvaus}";
                }
                break;

            case "TURN":
                uusiTila = "GAME";
                viesti = $"DATA {arvaus}";
                break;
        }

        return (uusiTila, viesti);
    }

    static void Main(string[] args)
    {
        string palvelimenOsoite = "127.0.0.1";
        int portti = 5000;
        int arvaus = args.Length > 0 ? int.Parse(args[0]) : 0;

        Socket udp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        udp.ReceiveTimeout = 5000;
        EndPoint palvelin = new IPEndPoint(IPAddress.Parse(palvelimenOsoite), portti);

        string tila = "CLOSED";
        string palvelimenViesti = "";

        Console.WriteLine("Peliasiakas käynnistyy...");

        while (tila != "CLOSED" || palvelimenViesti == "")
        {
            (tila, string lahetettava) = peliAsiakkaanViesti(tila, palvelimenViesti, arvaus);
            Console.WriteLine($"Tila: {tila}, Lähetetään: [{lahetettava}]");

            if (!string.IsNullOrEmpty(lahetettava))
            {
                byte[] data = Encoding.UTF8.GetBytes(lahetettava);
                udp.SendTo(data, palvelin);
                if (lahetettava.StartsWith("DATA")) arvaus++;
            }

            if (tila == "CLOSED" && !string.IsNullOrEmpty(lahetettava))
                break;

            // TURN: lähetetään oma arvaus heti, palvelin ei lähetä tässä välissä mitään
            if (tila != "TURN" && (!string.IsNullOrEmpty(lahetettava) || tila == "JOIN" || tila == "GAME"))
            {
                try
                {
                    byte[] buffer = new byte[1024];
                    EndPoint sender = new IPEndPoint(IPAddress.Any, 0);
                    int pituus = udp.ReceiveFrom(buffer, ref sender);
                    palvelimenViesti = Encoding.UTF8.GetString(buffer, 0, pituus).Trim();
                    Console.WriteLine($"Vastaanotettu: [{palvelimenViesti}]");
                }
                catch (SocketException)
                {
                    Console.WriteLine("Aikakatkaisu, ei vastausta palvelimelta.");
                    break;
                }
            }
            else
            {
                palvelimenViesti = "";
            }
        }

        udp.Close();
        Console.WriteLine("Peli päättyi.");
    }
}