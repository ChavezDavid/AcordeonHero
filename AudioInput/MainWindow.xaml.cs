using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NAudio;
using NAudio.Wave;
using NAudio.Dsp;

using Microsoft.Win32;

namespace AudioInput
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        WaveIn wavein;
        WaveFormat formato;
        WaveOutEvent waveOut;
        AudioFileReader reader;

        bool poder = false;

        int puntaje = 0;
        int puntosPoder = 0;
        int cronometro = 78000;

        double mDBoton;
        double mIBoton;
        double canvasLeft;
        double canvasRight;

        double pasoNota = Math.Pow(2, 1.0 / 12.0);
        double frecuenciaLaBase = 110.0;

        public MainWindow()
        {
            InitializeComponent();

            waveOut = new WaveOutEvent();

            Canvas.SetLeft(imgTrack, 80);
            Canvas.SetRight(imgTrack, 90);

            Canvas.SetLeft(imgBotonVerde, 80);
            Canvas.SetRight(imgBotonVerde, 130);

            canvasLeft = Canvas.GetLeft(imgTrack);
            canvasRight = Canvas.GetRight(imgTrack);

            lblMITrack.Text = Convert.ToString(canvasLeft);
            lblMDTrack.Text = Convert.ToString(canvasRight);
        }

        private void btnIniciar_Click(object sender, RoutedEventArgs e)
        {

            if (waveOut != null)
            {
                if (waveOut.PlaybackState == PlaybackState.Playing)
                {
                    waveOut.Stop();
                }
                waveOut.Init(reader);
                waveOut.Play();
            }

            wavein = new WaveIn();
            wavein.WaveFormat = new WaveFormat(44100, 16, 1);
            formato = wavein.WaveFormat;

            wavein.DataAvailable += OnDataAvailable;
            wavein.BufferMilliseconds = 500;

            wavein.StartRecording();
        }
        void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            byte[] buffer = e.Buffer;
            int bytesGrabados = e.BytesRecorded;

            double acumulador = 0;

            double nummuestras = bytesGrabados / 2;
            int exponente = 1;
            int numeroMuestrasComplejas = 0;
            int bitsMaximos = 0;

            do
            {
                bitsMaximos = (int)Math.Pow(2, exponente);
                exponente++;
            } while (bitsMaximos < nummuestras);

            exponente -= 2;
            numeroMuestrasComplejas = bitsMaximos / 2;

            Complex[] muestrasComplejas =
                new Complex[numeroMuestrasComplejas];

            for (int i = 0; i < bytesGrabados; i += 2)
            {
                short muestra = (short)(buffer[i + 1] << 8 | buffer[i]);

                float muestra32bits = (float)muestra / 32768.0f;

                acumulador += Math.Abs(muestra32bits);
                if (i / 2 < numeroMuestrasComplejas)
                {
                    muestrasComplejas[i / 2].X = muestra32bits;
                }
            }
            double promedio = acumulador / ((double)bytesGrabados / 2.0);

            if (promedio > 0)
            {
                FastFourierTransform.FFT(true, exponente, muestrasComplejas);
                float[] valoresAbsolutos =
                    new float[muestrasComplejas.Length];

                for (int i = 0; i < muestrasComplejas.Length; i++)
                {
                    valoresAbsolutos[i] = (float)
                        Math.Sqrt((muestrasComplejas[i].X * muestrasComplejas[i].X) +
                        (muestrasComplejas[i].Y * muestrasComplejas[i].Y));
                }

                int indiceMaximo =
                    valoresAbsolutos.ToList().IndexOf(
                        valoresAbsolutos.Max());

                float frecFundamental = (float)(indiceMaximo * wavein.WaveFormat.SampleRate) / (float)valoresAbsolutos.Length;

                lblFrecuencia.Text = frecFundamental.ToString("n2");

                agregarBoton();
                moverBoton();
                detectarFrecuencia(frecFundamental);

                int octava = 0;
                int indiceTono = (int)Math.Round(Math.Log10(frecFundamental / frecuenciaLaBase) / Math.Log10(pasoNota));
                if (indiceTono < 0)
                {
                    do
                    {
                        indiceTono += 12;
                        octava--;
                    } while (indiceTono < 0);
                }
                else if (indiceTono > 11)
                {
                    do
                    {
                        octava++;
                        indiceTono -= 12;
                    } while (indiceTono > 11);
                }
                double frecTono = frecuenciaLaBase;
                for (int i = 0; i < Math.Abs(octava); i++)
                {
                    if (octava > 0)
                    {
                        frecTono *= 2.0;
                    }
                    else if (octava < 0)
                    {
                        frecTono /= 2.0;
                    }
                }

                for (int i = 0; i < indiceTono; i++)
                {
                    frecTono *= Math.Pow(2, 1.0 / 12.0);
                }

                double proxTono = frecTono * Math.Pow(2, 1.0 / 12.0);
                double antTono = frecTono / Math.Pow(2, 1.0 / 12.0);
                double rango = proxTono - antTono;
                double frecNormalizada = (frecFundamental - antTono) / rango;
            } else
            {
                lblFrecuencia.Text = "0";
            }   
        }

        void moverBoton()
        {
            mIBoton = Canvas.GetLeft(imgBotonVerde);
            lblMIBoton.Text = Convert.ToString(mIBoton);

            mDBoton = Canvas.GetRight(imgBotonVerde);
            lblMDBoton.Text = Convert.ToString(mDBoton);

            mIBoton += 50;
            mDBoton += 50;
            Canvas.SetLeft(imgBotonVerde, mIBoton);
            Canvas.SetRight(imgBotonVerde, mDBoton);

        }

        void detectarFrecuencia(float frecFundamental)
        {
            int boton = 0;

            if (frecFundamental > 760 && frecFundamental < 920)
            {
                boton = 1;
            }
            if (frecFundamental > 473 && frecFundamental < 503)
            {
                boton = 2;
            }
            if (frecFundamental > 513 && frecFundamental < 543)
            {
                boton = 3;
            }
            if (frecFundamental > 567 && frecFundamental < 607)
            {
                boton = 4;
            }
            if (frecFundamental > 639 && frecFundamental < 669)
            {
                boton = 5;
            }
            if (frecFundamental > 688 && frecFundamental < 718)
            {
                boton = 6;
            }
            if (frecFundamental > 12000 && frecFundamental < 13000)
            {
                boton = 7;
            }

            pulsarBoton(boton);
        }

        void pulsarBoton(int boton)
        {
            bool acierto = false;

            if (boton == 1)
            {
                if (mIBoton <= 634 && mDBoton >= 588)
                {
                    acierto = true;
                }
            }
            if (boton == 2 && mIBoton <= 634 && mDBoton >= 588)
            {
                acierto = true;
            }
            if (boton == 3 && mIBoton <= 634 && mDBoton >= 588)
            {
                acierto = true;
            }
            if (boton == 4 && mIBoton <= 634 && mDBoton >= 588)
            {
                acierto = true;
            }
            if (boton == 5 && mIBoton <= 634 && mDBoton >= 588)
            {
                acierto = true;
            }
            if (boton == 6 && mIBoton <= 634 && mDBoton >= 588)
            {
                acierto = true;
            }
            if (boton == 7)
            {
                poder = true;
            }

            if (acierto)
            {
                puntaje += 100;
                puntosPoder += 1;
            }

            if (poder && puntosPoder >= 10)
            {
                activarPoder();
            }

            acierto = false;
            lblPuntaje.Text = Convert.ToString(puntaje);
        }

        void activarPoder()
        {
            lblPoder.Text = "Activado";
            if (cronometro <= 0)
            {
                lblPoder.Text = "Desactivado";
                cronometro = 100;
                poder = false;
            }
            cronometro -= 1;
        }

        void agregarBoton()
        {
            Image verde = new Image();
            verde.Source = new BitmapImage(new Uri(@"graficos/Green.png", UriKind.RelativeOrAbsolute));
            Canvas.SetLeft(verde, 200);
            gridPrincipal.Children.Add(verde);
        }

        private void btnFinalizar_Click(object sender, RoutedEventArgs e)
        {
            wavein.StopRecording();
        }

        private void btnExaminar_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            if ((bool)fileDialog.ShowDialog())
            {
                txtRuta.Text = fileDialog.FileName;
                reader = new AudioFileReader(fileDialog.FileName);
            }
        }
    }
}
