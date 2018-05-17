using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioInput
{
    class Nota
    {
        double momento = 0;
        int boton = 0;

        public Nota() { }

        public void setMomento(double momento)
        {
            this.momento = momento;
        }

        public double getMomento()
        {
            return momento;
        }

        public void setBoton(int boton)
        {
            this.boton = boton;
        }

        public int getBoton()
        {
            return boton;
        }
    }
}
