using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Linq;

public class Pelaaja
{
    private EndPoint[] _pelaaja = new EndPoint[2];
    public EndPoint this[int index]
    {
        get => _pelaaja[index];
        set => _pelaaja[index] = value;
    }
}

public class Nimi
{
    private string[] _nimi = new string[2];
    public string this[int index]
    {
        get => _nimi[index];
        set => _nimi[index] = value;
    }
}

public class Viesti
{
    private string[] _viesti = new string[2];
    public string this[int index]
    {
        get => _viesti[index];
        set => _viesti[index] = value;
    }
}

class Peli
{
    private static int maxNum = 6;
    private bool debug { get; set; }
    private int oikea { get; set; }
    private int vuoro { get; set; }
    private int pelaajia { get; set; }
    private int loppukuittauksia { get; set; }

    private enum Tilat { CLOSED, WAIT, GAME, WAIT_ACK, END }
    private Tilat tila;
    private Pelaaja pelaajat;
    private Nimi nimet;
    private Viesti viestit;

    public Peli()
    {
        tila = Tilat.CLOSED;
        debug = false;
        Random randi = new Random();
        oikea = randi.Next(0, maxNum);
        pelaajat = new Pelaaja();
        nimet = new Nimi();
        viestit = new Viesti();
        pelaajia = 0;
        tila = Tilat.WAIT;
    }

    public string GetViesti(int i) => viestit[i];
    public EndPoint GetPelaaja(int i) => pelaajat[i];

    public (string, EndPoint) TulkitseViesti(string viesti, EndPoint remote)
    {
        debug = true;
        string[] osat = viesti.Split(' ');
        viestit[0] = "";
        viestit[1] = "";
        switch (tila)
        {
            case Tilat.CLOSED:
            {
                tila = Tilat.WAIT;
                if (debug) Console.WriteLine("CLOSED -> WAIT");
                break;
            }
            case Tilat.WAIT:
            {
                if (osat[0] == "JOIN")
                {
                    int indeksi = pelaajia;
                    pelaajat[indeksi] = remote;
                    nimet[indeksi] = string.Join(" ", osat, 1, osat.Length - 1);
                    pelaajia++;
                    if (pelaajia == 1)
                    {
                        viestit[0] = "ACK 201 JOIN OK";
                        if (debug) Console.WriteLine($"WAIT: 1. pelaaja liittyi, ACK 201");
                    }
                    else if (pelaajia == 2)
                    {
                        Random rnd = new Random();
                        vuoro = rnd.Next(0, 2);
                        oikea = rnd.Next(0, maxNum);
                        int odottaja = 1 - vuoro;
                        viestit[vuoro]    = "ACK 202 " + nimet[odottaja];
                        viestit[odottaja] = "ACK 203 " + nimet[vuoro];
                        tila = Tilat.GAME;
                        if (debug) Console.WriteLine($"WAIT -> GAME: vuoro={vuoro}, oikea={oikea}");
                    }
                }
                break;
            }
            case Tilat.GAME:
            {
                if (osat[0] == "DATA")
                {
                    int arvaus = int.Parse(osat[1]);
                    int vastustaja = 1 - vuoro;
                    if (arvaus == oikea)
                    {
                        viestit[vuoro]      = "QUIT 501";
                        viestit[vastustaja] = "QUIT 502";
                        loppukuittauksia = 0;
                        tila = Tilat.END;
                    }
                    else
                    {
                        viestit[vuoro]      = "ACK 300";
                        viestit[vastustaja] = $"DATA {arvaus}";
                        vuoro = vastustaja;
                        tila = Tilat.WAIT_ACK;
                    }
                }
                break;
            }
            case Tilat.WAIT_ACK:
            {
                if (osat[0] == "ACK" && remote.Equals(pelaajat[vuoro]))
                {
                    tila = Tilat.GAME;
                }
                break;
            }
            case Tilat.END:
            {
                if (osat[0] == "ACK")
                {
                    loppukuittauksia++;
                    if (debug) Console.WriteLine($"END: kuittaus {loppukuittauksia}/{pelaajia}");
                    if (loppukuittauksia >= pelaajia)
                    {
                        tila = Tilat.CLOSED;
                        pelaajia = 0;
                        loppukuittauksia = 0;
                        if (debug) Console.WriteLine("END -> CLOSED");
                    }
                }
                break;
            }
        }
        return (viesti, remote);
    }
}

class Palvelin
{
    static void Main(string[] args)
    {
        int portti = 5000;
        Socket udp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        udp.Bind(new IPEndPoint(IPAddress.Any, portti));
        Console.WriteLine($"Pelipalvelin käynnissä portissa {portti}");

        Peli peli = new Peli();

        while (true)
        {
            try
            {
                byte[] buffer = new byte[1024];
                EndPoint sender = new IPEndPoint(IPAddress.Any, 0);
                int pituus = udp.ReceiveFrom(buffer, ref sender);
                string viesti = Encoding.UTF8.GetString(buffer, 0, pituus).Trim();
                Console.WriteLine($"Vastaanotettu [{viesti}] osoitteesta {sender}");

                string[] osat = viesti.Split(' ');

                string[] tunnetut = { "JOIN", "DATA", "ACK" };
                if (string.IsNullOrWhiteSpace(viesti) || !tunnetut.Contains(osat[0]))
                {
                    byte[] virheData = Encoding.UTF8.GetBytes("ACK 407");
                    udp.SendTo(virheData, sender);
                    Console.WriteLine($"Tuntematon viesti [{viesti}], lähetetty ACK 407");
                    continue;
                }

                peli.TulkitseViesti(viesti, sender);

                for (int i = 0; i < 2; i++)
                {
                    string msg = peli.GetViesti(i);
                    EndPoint ep = peli.GetPelaaja(i);
                    if (!string.IsNullOrEmpty(msg) && ep != null)
                    {
                        byte[] laheta = Encoding.UTF8.GetBytes(msg);
                        udp.SendTo(laheta, ep);
                        Console.WriteLine($"Lähetetty [{msg}] osoitteeseen {ep}");
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Virhe: {e.Message}");
            }
        }
        udp.Close();
    }
}