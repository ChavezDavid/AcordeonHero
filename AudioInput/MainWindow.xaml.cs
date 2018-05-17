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

using System.Windows.Threading;

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

        Nota[] notaMorada = new Nota[9];
        int contadorNotaMorada = 0;

        Image[] morado = new Image[9];
        int contadorMorado = 0;

        bool poder = false;

        int puntaje = 0;
        int puntosPoder = 0;
        int cronometro = 100;

        double mDBoton;
        double mIBoton;
        double canvasLeft;
        double canvasRight;

        double pasoNota = Math.Pow(2, 1.0 / 12.0);
        double frecuenciaLaBase = 110.0;

        DispatcherTimer timer;

        public MainWindow()
        {
            InitializeComponent();

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Tick += OnTimerTick;

            notaMorada[contadorNotaMorada] = new Nota();

            notaMorada[contadorNotaMorada].setMomento(210);
            notaMorada[contadorNotaMorada].setBoton(1);

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

        private void OnTimerTick(object sender, EventArgs e)
        {
            if (reader != null)
            {
                string tiempoActual = reader.CurrentTime.ToString();
                tiempoActual = tiempoActual.Substring(0, 8);
                lblTiempo.Text = tiempoActual.Substring(0, 8);

                
                moverBoton();
                
            }
        }

        private void btnIniciar_Click(object sender, RoutedEventArgs e)
        {

            timer.Start();

            aparecerNotaMorado(notaMorada[contadorNotaMorada]);

            if (waveOut != null)
            {
                if (waveOut.PlaybackState == PlaybackState.Playing)
                {
                    waveOut.Stop();
                }
                reader = new AudioFileReader("graficos\\Tragos.wav");
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

                //agregarBoton();
                //moverBoton();
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

            mIBoton += 23.4;
            mDBoton += 23.4;
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
            if (frecFundamental > 1000 && frecFundamental < 1200)
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
                imgBotonVerde.Source = new BitmapImage(new Uri(@"graficos/Green.png", UriKind.RelativeOrAbsolute));
                acierto = true;
            }
            if (boton == 2)
            {
                imgBotonVerde.Source = new BitmapImage(new Uri(@"graficos/Red.png", UriKind.RelativeOrAbsolute));
                acierto = true;
            }
            if (boton == 3)
            {
                imgBotonVerde.Source = new BitmapImage(new Uri(@"graficos/Yellow.png", UriKind.RelativeOrAbsolute));
                acierto = true;
            }
            if (boton == 4)
            {
                imgBotonVerde.Source = new BitmapImage(new Uri(@"graficos/Blue.png", UriKind.RelativeOrAbsolute));
                acierto = true;
            }
            if (boton == 5)
            {
                imgBotonVerde.Source = new BitmapImage(new Uri(@"graficos/Orange.png", UriKind.RelativeOrAbsolute));
                acierto = true;
            }
            if (boton == 6)
            {
                imgBotonVerde.Source = new BitmapImage(new Uri(@"graficos/Pink.png", UriKind.RelativeOrAbsolute));
                acierto = true;
            }
            if (boton == 7)
            {
                imgBotonVerde.Source = new BitmapImage(new Uri(@"graficos/NotaEspecial.png", UriKind.RelativeOrAbsolute));
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
            imgCaguama.Source = new BitmapImage(new Uri(@"graficos/Caguama.png", UriKind.RelativeOrAbsolute));
            if (cronometro <= 0)
            {
                lblPoder.Text = "Desactivado";
                cronometro = 100;
                poder = false;
                puntosPoder = 0;
                imgCaguama.Source = new BitmapImage(new Uri(@"graficos/Charrones.png", UriKind.RelativeOrAbsolute));
            }
            cronometro -= 1;
        }

        void aparecerNotaMorado(Nota nota)
        {
            morado[contadorMorado] = new Image();
            morado[contadorMorado].Source = new BitmapImage(new Uri(@"graficos/Purple.png", UriKind.RelativeOrAbsolute));
            morado[contadorMorado].Width = 50;
            morado[contadorMorado].Height = 50;
            Canvas.SetLeft(morado[contadorMorado], 0);
            Canvas.SetRight(morado[contadorMorado], 50);

            if (notaMorada[contadorNotaMorada].getBoton() == 1)
            {
                gridPrincipal.Children.Add(morado[contadorMorado]);
            }
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
            waveOut.Stop();
            wavein.StopRecording();
        }
    }
}
