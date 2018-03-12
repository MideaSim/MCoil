﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Basic
{
    public class RefrigerantHTCandDP
    {
        public static RefHTCandDPResult HTCandDP(string[] fluid, double[] composition, double d, double g, double p, double x, double l, double q, double zh, double zdp, double hexType)
        {
            //Main function for 2-phase flow HTC and calculation
            double href = 0;
            double PressD = 0;

            //*************Smoothening HTC & DP at single-to-2 phase transitions****************
            //********divided to 2 region based on quality (0 - 0.95 - 1) to calculated HTC & DP********
            if (hexType == 0)
            {
                if (x <= 0.95 && x >= 0)
                {
                    href = RefrigerantTPHTC.Shah_Evap_href(fluid, composition, d, g, p, x, q, l);
                    //href = RefrigerantTPHTC.Kandlikar_Evap_href(fluid, composition, d, g, p, x, q, l);
                    //href = RefrigerantTPHTC.JR_Evap_href(fluid, composition, d, g, p, x, q, l);
                    PressD = RefrigerantTPDP.deltap_smooth(fluid, composition, d, g, p, x, l);
                    //PressD = RefrigerantTPDP.deltap_JR(fluid, composition, d, g, p, x, l);
                    //PressD = RefrigerantTPDP.deltap_MS(fluid, composition, d, g, p, x, l);
                }

                if (x < 1 && x > 0.95) //vapor to 2-phase
                {
                    href = RefrigerantTPHTC.Shah_Evap_href(fluid, composition, d, g, p, x, q, l);
                    //href = RefrigerantTPHTC.Kandlikar_Evap_href(fluid, composition, d, g, p, x, q, l);
                    //href = RefrigerantTPHTC.JR_Evap_href(fluid, composition, d, g, p, x, q, l);
                    PressD = RefrigerantTPDP.deltap_smooth(fluid, composition, d, g, p, x, l);
                    //PressD = RefrigerantTPDP.deltap_JR(fluid, composition, d, g, p, x, l);
                    //PressD = RefrigerantTPDP.deltap_MS(fluid, composition, d, g, p, x, l);
                }

                //********divided to 4 region based on quality (0 - 0.05 - 0.9 - 0.95 - 1) to calculated HTC & DP END********
            }
            else
            {
                if (x <= 0.95 && x >= 0)
                {
                    href = RefrigerantTPHTC.Shah_Cond_href(fluid, composition, d, g, p, x, q, l);
                    //href = RefrigerantTPHTC.Dobson_Cond_href(fluid, composition, d, g, p, x, Ts, l);//需要考虑Ts的参数传递
                    PressD = RefrigerantTPDP.deltap_smooth(fluid, composition, d, g, p, x, l);
                    //PressD = RefrigerantTPDP.deltap_JR(fluid, composition, d, g, p, x, l);
                    //PressD = RefrigerantTPDP.deltap_MS(fluid, composition, d, g, p, x, l);
                }

                if (x < 1 && x > 0.95) //vapor to 2-phase
                {
                    href = RefrigerantTPHTC.Shah_Cond_href(fluid, composition, d, g, p, x, q, l);
                    //href = RefrigerantTPHTC.Dobson_Cond_href(fluid, composition, d, g, p, x, Ts, l);//需要考虑Ts的参数传递
                    PressD = RefrigerantTPDP.deltap_smooth(fluid, composition, d, g, p, x, l);
                    //PressD = RefrigerantTPDP.deltap_JR(fluid, composition, d, g, p, x, l);
                    //PressD = RefrigerantTPDP.deltap_MS(fluid, composition, d, g, p, x, l);
                }

                //********divided to 4 region based on quality (0 - 0.05 - 0.9 - 0.95 - 1) to calculated HTC & DP END********
                
            }

            href = href * zh;
            PressD = PressD * zdp;

            return new RefHTCandDPResult { Href = href, DPref = PressD };

        }
   
    }
}