using HtmlAgilityPack;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace _02_namaz_vakti
{
    public partial class formVakitci : Form
    {
        private string progAd = "BT Vakitçi";
        public formVakitci()
        {
            InitializeComponent();

            baslataEkle(progAd);
        }

        #region Global Değişkenler:
        private bool formuTasi = false;
        private bool calisiyorMu = true;
        Point ilkKonum = new Point(0, 0);

        void zamaniHesapla(string nmzVakit, int sonraSaat, int sonraDakika, int suanSaat, int suanDakika, int suanSaniye)
        {
            int iSaat1;
            int iDakika1;
            int iSaniye1;
            iSaat1 = sonraSaat - suanSaat;
            iDakika1 = sonraDakika - suanDakika;
            iSaniye1 = 59 - suanSaniye;

            if (iDakika1 <= 0)
            {
                iDakika1 += 60;
                iSaat1--;
            }
            else
            {
                iDakika1--;
            }

            string klnSaat = (iSaat1).ToString();
            string klnDakika = (iDakika1).ToString();
            string klnSaniye = (iSaniye1).ToString();
            if (iSaat1 < 10 && iSaat1 >= 0)
                klnSaat = "0" + klnSaat;
            if (iDakika1 < 10 && iDakika1 >= 0)
                klnDakika = "0" + klnDakika;
            if (iSaniye1 < 10 && iSaniye1 >= 0)
                klnSaniye = "0" + klnSaniye;

            labelKalanSure.Text = klnSaat + ":" + klnDakika + ":" + klnSaniye;

            if (suanSaat == sonraSaat && suanDakika == sonraDakika && (suanSaniye == 00 || suanSaniye == 05 || suanSaniye == 10))
                notifyMesaj("Namaz Vakti", nmzVakit + " vaktine girildi!", "uyari");
        }

        void baslataEkle(string progAd)
        {
            try
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
                if (key.GetValue(progAd).ToString() == "\"" + Application.ExecutablePath + "\"")
                {
                    checkBasCalistir.Checked = true;
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private bool pinSay = true;
        void pinKontrol()
        {
            if (pinSay)
            {
                picBoxPin.Image = Properties.Resources.pin;
                this.TopMost = true;
                pinSay = false;
            }
            else
            {
                picBoxPin.Image = Properties.Resources.unpin;
                this.TopMost = false;
                pinSay = true;
            }
        }

        private bool iconBak = false;
        void iconKontrol()
        {
            if (iconBak)
            {
                this.Show();
                iconBak = false;
            }
            else
            {
                this.Hide();
                iconBak = true;
            }
        }

        void progKontrol()
        {
            if (Process.GetProcessesByName(Assembly.GetEntryAssembly().GetName().Name).Count() > 1)
            {
                acilirMesaj("Bu program zaten çalışıyor. \nTekrardan çalıştıramazsınız!", "UYARI", "uyarı");
                this.Close();
            }
        }

        void acilirMesaj(string metin, string baslik, string durum)
        {
            if (durum == "bilgi")
                MessageBox.Show(metin, baslik, MessageBoxButtons.OK, MessageBoxIcon.Information);
            else if (durum == "uyarı")
                MessageBox.Show(metin, baslik, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            else if (durum == "hata")
                MessageBox.Show(metin, baslik, MessageBoxButtons.OK, MessageBoxIcon.Error);
            else if (durum == "soru")
                MessageBox.Show(metin, baslik, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            else
                MessageBox.Show(metin, baslik, MessageBoxButtons.YesNo, MessageBoxIcon.Information);
        }

        void notifyMesaj(string baslik, string metin, string durum)
        {
            if (durum == "bilgi")
                this.notifyIcon1.ShowBalloonTip(0xea60, baslik, metin, ToolTipIcon.Info);
            else if (durum == "uyarı")
                this.notifyIcon1.ShowBalloonTip(0xea60, baslik, metin, ToolTipIcon.Warning);
            else if (durum == "hata")
                this.notifyIcon1.ShowBalloonTip(0xea60, baslik, metin, ToolTipIcon.Error);
            else
                this.notifyIcon1.ShowBalloonTip(0xea60, baslik, metin, ToolTipIcon.Info);
        }
        #endregion

        #region formVakitci_Load():
        private void formVakitci_Load(object sender, EventArgs e)
        {
            if (NetworkInterface.GetIsNetworkAvailable() == false)
            {
                string netHata = "İnternet bağlantınız olmadığından program başlatılamıyor. \nBu programı kullanabilmeniz için internet bağlantınız olması gerekmektedir!";
                acilirMesaj(netHata, "İnternet Yok !", "hata");
                notifyIcon1.Visible = false;
                this.Close();
            }
            else
            {
                //this.Hide();
                timerSaat.Start();
                timerKalanSure.Start();

                progKontrol();
                pinKontrol();

                comboDil.Enabled = false;
                gosterToolStripMenuItem.Visible = false;

                Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("tr-TR");
                labelSisSaat.Text = DateTime.Now.ToLongTimeString();
                labelSisTarih.Text = DateTime.Now.ToLongDateString();

                this.Width = 263;
                this.Height = 476;
                this.Left = Screen.PrimaryScreen.WorkingArea.Right - 2 - this.Width;
                this.Top = Screen.PrimaryScreen.WorkingArea.Bottom - 2 - this.Height;

                string nmzVakit;
                int iSaat;
                int iDakika;
                int iSaniye;
                var sSaat = ZamaniCekIsle(out var sDakika, out var sSaniye, out var imSaat, out var guSaat, out var ogSaat, out var ikSaat, out var akSaat, out var yaSaat, out var imDakika, out var guDakika, out var ogDakika, out var ikDakika, out var akDakika, out var yaDakika);

                int deger = Convert.ToInt32(textSeffafflik.Text);
                this.Opacity = 0.01 * deger;

                if (checkSimgeGoster.Checked == true)
                    this.ShowInTaskbar = true;
                else
                    this.ShowInTaskbar = false;
            }
        }
        #endregion

        #region formVakitci_Mouse():
        private void formVakitci_MouseDown(object sender, MouseEventArgs e)
        {
            formuTasi = true;
            ilkKonum = new Point(e.X, e.Y);
        }

        private void formVakitci_MouseUp(object sender, MouseEventArgs e)
        {
            formuTasi = false;
        }

        private void formVakitci_MouseMove(object sender, MouseEventArgs e)
        {
            if (formuTasi)
            {
                Point p = PointToScreen(e.Location);
                Location = new Point(p.X - this.ilkKonum.X, p.Y - this.ilkKonum.Y);
            }
        }
        #endregion

        #region linkLabel_Clicked():
        private void linkLabelFirma1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            linkLabelFirma1.LinkVisited = true;
            System.Diagnostics.Process.Start("https://bilinentasarim.com");
        }

        private void linkLabelFirma2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            linkLabelFirma2.LinkVisited = true;
            System.Diagnostics.Process.Start("https://bilinentasarim.com");
        }
        #endregion

        #region ToolStripMenuItem_Click():
        private void gosterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (calisiyorMu == false)
            {
                this.Show();
                calisiyorMu = true;
                gizleToolStripMenuItem.Visible = true;
                gosterToolStripMenuItem.Visible = false;
            }
        }

        private void gizleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (calisiyorMu)
            {
                this.Hide();
                notifyMesaj("Programı Gizleme", "Bu program arkaplanda çalışmaya devam edecektir...", "bilgi");
                calisiyorMu = false;
                gosterToolStripMenuItem.Visible = true;
                gizleToolStripMenuItem.Visible = false;
            }

        }

        private void kapatToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        #endregion

        #region ZamaniCekIsle():
        private int ZamaniCekIsle(out int sDakika, out int sSaniye, out int imSaat, out int guSaat, out int ogSaat,
            out int ikSaat, out int akSaat, out int yaSaat, out int imDakika, out int guDakika, out int ogDakika,
            out int ikDakika, out int akDakika, out int yaDakika)
        {
            #region ZAMANI ÇEK:
            Uri url = new Uri("https://namazvakitleri.diyanet.gov.tr/tr-TR/9541/istanbul-icin-namaz-vakti");
            WebClient client = new WebClient();
            string html = client.DownloadString(url);
            HtmlAgilityPack.HtmlDocument dokuman = new HtmlAgilityPack.HtmlDocument();
            dokuman.LoadHtml(html);
            HtmlNodeCollection zamanlar = dokuman.DocumentNode.SelectNodes("//div[@class='tpt-time']");
            HtmlNodeCollection sonrakiVakit = dokuman.DocumentNode.SelectNodes("//div[@id='remainingTimeField']");
            listBox1.Items.Clear();
            #endregion

            #region ZAMANI İŞLE:
            foreach (var zaman in zamanlar)
                listBox1.Items.Add(zaman.InnerText);

            for (int i = 1; i <= 6; i++)
            {
                switch (i)
                {
                    case 1:
                        labelSaatImsak.Text = listBox1.Items[1].ToString();
                        break;
                    case 2:
                        labelSaatGunes.Text = listBox1.Items[2].ToString();
                        break;
                    case 3:
                        labelSaatOgle.Text = listBox1.Items[3].ToString();
                        break;
                    case 4:
                        labelSaatIkindi.Text = listBox1.Items[4].ToString();
                        break;
                    case 5:
                        labelSaatAksam.Text = listBox1.Items[5].ToString();
                        break;
                    case 6:
                        labelSaatYatsi.Text = listBox1.Items[6].ToString();
                        break;
                }
            }

            string[] suankiZaman = labelSisSaat.Text.Split(':');
            string[] imSaati = labelSaatImsak.Text.Split(':');
            string[] guSaati = labelSaatGunes.Text.Split(':');
            string[] ogSaati = labelSaatOgle.Text.Split(':');
            string[] ikSaati = labelSaatIkindi.Text.Split(':');
            string[] akSaati = labelSaatAksam.Text.Split(':');
            string[] yaSaati = labelSaatYatsi.Text.Split(':');
            string nmzVakit = "", klnSaat = "", klnDakika = "", klnSaniye = "";
            int iSaat = 0, iDakika = 0, iSaniye = 0;

            int sSaat = Convert.ToInt32(suankiZaman[0]);
            sDakika = Convert.ToInt32(suankiZaman[1]);
            sSaniye = Convert.ToInt32(suankiZaman[2]);

            imSaat = Convert.ToInt32(imSaati[0]);
            guSaat = Convert.ToInt32(guSaati[0]);
            ogSaat = Convert.ToInt32(ogSaati[0]);
            ikSaat = Convert.ToInt32(ikSaati[0]);
            akSaat = Convert.ToInt32(akSaati[0]);
            yaSaat = Convert.ToInt32(yaSaati[0]);

            imDakika = Convert.ToInt32(imSaati[1]);
            guDakika = Convert.ToInt32(guSaati[1]);
            ogDakika = Convert.ToInt32(ogSaati[1]);
            ikDakika = Convert.ToInt32(ikSaati[1]);
            akDakika = Convert.ToInt32(akSaati[1]);
            yaDakika = Convert.ToInt32(yaSaati[1]);
            #endregion

            return sSaat;
        }
        #endregion

        #region timerKalanSure_Tick():
        private void timerKalanSure_Tick(object sender, EventArgs e)
        {
            string nmzVakit = "";
            int iSaat;
            int iDakika;
            int iSaniye;
            var sSaat = ZamaniCekIsle(out var sDakika, out var sSaniye, out var imSaat, out var guSaat, out var ogSaat, out var ikSaat, out var akSaat, out var yaSaat, out var imDakika, out var guDakika, out var ogDakika, out var ikDakika, out var akDakika, out var yaDakika);

            #region ZAMANI ÇÖZÜMLE:
            if (sSaat >= imSaat && sSaat < guSaat && sSaat < ogSaat && sSaat < ikSaat && sSaat < akSaat && (sSaat < yaSaat || sSaat > yaSaat))
            {
                groupImsak.BackColor = Color.FromArgb(37, 190, 139);
                groupYatsi.BackColor = Color.White;
                nmzVakit = lblImsak.Text;
                labelGelecekVakit.Text = lblGunes.Text + " 'e Kalan Süre";

                zamaniHesapla(nmzVakit, guSaat, guDakika, sSaat, sDakika, sSaniye);
            }

            if (sSaat > imSaat && sSaat >= guSaat && sSaat < ogSaat && sSaat < ikSaat && sSaat < akSaat && sSaat < yaSaat)
            {
                groupGunes.BackColor = Color.FromArgb(37, 190, 139);
                groupImsak.BackColor = Color.White;
                nmzVakit = lblGunes.Text;
                labelGelecekVakit.Text = lblOgle.Text + " 'ye Kalan Süre";

                zamaniHesapla(nmzVakit, ogSaat, ogDakika, sSaat, sDakika, sSaniye);
            }

            if (sSaat > imSaat && sSaat > guSaat && sSaat >= ogSaat && sSaat < ikSaat && sSaat < akSaat && sSaat < yaSaat)
            {
                groupOgle.BackColor = Color.FromArgb(37, 190, 139);
                groupGunes.BackColor = Color.White;
                nmzVakit = lblOgle.Text;
                labelGelecekVakit.Text = lblIkindi.Text + " 'ye Kalan Süre";

                zamaniHesapla(nmzVakit, ikSaat, ikDakika, sSaat, sDakika, sSaniye);
            }

            if (sSaat > imSaat && sSaat > guSaat && sSaat > ogSaat && sSaat >= ikSaat && sSaat < akSaat && sSaat < yaSaat)
            {
                groupIkindi.BackColor = Color.FromArgb(37, 190, 139);
                groupOgle.BackColor = Color.White;
                nmzVakit = lblIkindi.Text;
                labelGelecekVakit.Text = lblAksam.Text + " 'a Kalan Süre";

                zamaniHesapla(nmzVakit, akSaat, akDakika, sSaat, sDakika, sSaniye);
            }

            if (sSaat > imSaat && sSaat > guSaat && sSaat > ogSaat && sSaat > ikSaat && sSaat >= akSaat && sSaat < yaSaat)
            {
                groupAksam.BackColor = Color.FromArgb(37, 190, 139);
                groupIkindi.BackColor = Color.White;
                nmzVakit = lblAksam.Text;
                labelGelecekVakit.Text = lblYatsi.Text + " 'ya Kalan Süre";

                zamaniHesapla(nmzVakit, yaSaat, yaDakika, sSaat, sDakika, sSaniye);
            }

            if (sSaat <= imSaat && sSaat < guSaat && sSaat < ogSaat && sSaat < ikSaat && sSaat < akSaat && sSaat < yaSaat)
            {
                groupYatsi.BackColor = Color.FromArgb(37, 190, 139);
                groupAksam.BackColor = Color.White;
                nmzVakit = lblYatsi.Text;
                labelGelecekVakit.Text = lblImsak.Text + " 'a Kalan Süre";

                zamaniHesapla(nmzVakit, imSaat, imDakika, sSaat, sDakika, sSaniye);
            }

            #endregion
        }
        #endregion

        #region allButton_Click():
        private void buttonSefArti_Click(object sender, EventArgs e)
        {
            int deger = Convert.ToInt32(textSeffafflik.Text);
            this.Opacity = 0.01 * deger;

            if (deger < 100)
            {
                deger += 5;
                textSeffafflik.Text = deger.ToString();
                this.Opacity += 0.05;
            }
        }

        private void buttonSefEksi_Click(object sender, EventArgs e)
        {
            int deger = Convert.ToInt32(textSeffafflik.Text);
            this.Opacity = 0.01 * deger;

            if (deger > 0)
            {
                deger -= 5;
                textSeffafflik.Text = deger.ToString();
                this.Opacity -= 0.05;
            }
        }

        private void buttonAyarKaydet_Click(object sender, EventArgs e)
        {
            if (checkBasCalistir.Checked)
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
                key.SetValue(progAd, "\"" + Application.ExecutablePath + "\"");
            }
            else
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
                key.DeleteValue(progAd);
            }

            acilirMesaj("Ayarlarınız başarıyla kaydedildi!", "Ayar Kaydetme", "bilgi");

            TabPage tab = tabVakitci.TabPages[0];
            tabVakitci.SelectTab(tab);
        }
        #endregion

        private void formVakitci_FormClosing(object sender, FormClosingEventArgs e)
        {
            notifyIcon1.Visible = false;
        }

        private void timerSaat_Tick(object sender, EventArgs e)
        {
            labelSisSaat.Text = DateTime.Now.ToLongTimeString();
        }

        private void picBoxClose_Click(object sender, EventArgs e)
        {
            notifyMesaj("Çalışma Durumu", "Bu program arkaplanda çalışmaya devam edecektir...", "bilgi");
            gosterToolStripMenuItem.Visible = true;
            gizleToolStripMenuItem.Visible = false;
            calisiyorMu = false;
            this.Hide();
        }

        private void picBoxPin_Click(object sender, EventArgs e)
        {
            pinKontrol();
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            iconKontrol();
        }
    }
}
