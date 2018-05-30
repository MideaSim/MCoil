﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Model.Basic;
using System.IO;

namespace Model
{
    public class Slab
    {
        public static CalcResult SlabCalc(int[,] CirArrange, CircuitNumber CircuitInfo, int Nrow, int[] Ntube, int Nelement, string fluid, //double Npass, int[] N_tubes_pass, 
            double dh, double l, AreaResult geo, double[, ,] ta, double[, ,] RH,
            double te, double pe, double hri, double mr, double[,] ma, double[,] ha,
            double eta_surface, double zh, double zdp, int hexType, double thickness, double conductivity, double Pwater,string Airdirection)
   
        {
           //------->        
           // R2   R1

           // [11   1] <====
           // [12   2] <====
           //          <==== Air
           // [13   3] <====
           // [14   4] <====
           // [15   5]  <====
           // [16   6] <====
           // [17   7] <====
           // [18   8]  <====         
           // [19   9] <====
           // [20  10] <====

           //  Ncir=1, 11in, 20->10 1out


            // [19 - 17 - 15 - 13   11   9   7   5   3   1] <====Air
            // [20 - 18 - 16 - 14   12   10  8   6   4   2] <====Air
            //  Ncir=1, 20in, 20->19 1out

           // CirArrange = new int[,] { { 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 } };
           // Nrow=2
           // Ncir=2
            double[] Q1 = new double[50];
            double tri = te; //Refrigerant.SATP(fluid, composition, pri, 1).Temperature - 273.15;
            double pri = pe;
            int Nciri = CircuitInfo.number[0];
            int Nciro = CircuitInfo.number[1];
            int Ncir = (Nciri == Nciro ? Nciri : Nciri + Nciro);
             
            int N_tube = Ntube[0];
            int N_tube_total = 0;
            CirArr[] cirArr = new CirArr[Nrow * N_tube];
            CirArrforAir cirArrforAir = new CirArrforAir();
            cirArrforAir = CirArrangement.ReadCirArr(CirArrange, CircuitInfo, Nrow, Ntube);
            cirArr = cirArrforAir.CirArr;

            CalcResult res_slab = new CalcResult();

            double[] pri_cir = new double[Ncir]; //[element, tube, row]
            double[] hri_cir = new double[Ncir];
            double[] tri_cir = new double[Ncir];
            double[] mr_ciri = new double[Nciri];
            List<double[]> mr_ciri_base = new List<double[]>();
         
            double[] mr_ciro = new double[Nciro];
            int[] Ngroupin = new int[Nciro];
            int index = 0;
            int restartDP_index = 0;
            int N_tube2 = 0;
            int[] index_cir = new int[Ncir];
            int index_mr_ciri_base = 0;
            CalcResult[] r = new CalcResult[Ncir];
            CalcResult[] r1 = new CalcResult[Ncir];
            CalcResult[] r2 = new CalcResult[Ncir]; //for NinMout only
            CalcResult[] res_cir2 = new CalcResult[Nciro + 1];

            double[,] Q_detail=new double[N_tube,Nrow];//detail output
            double[,] DP_detail = new double[N_tube, Nrow];
            double[,] Tri_detail = new double[N_tube, Nrow];
            double[,] Pri_detail = new double[N_tube, Nrow];
            double[,] hri_detail = new double[N_tube, Nrow];
            double[,] Tro_detail = new double[N_tube, Nrow];
            double[,] Pro_detail = new double[N_tube, Nrow];
            double[,] hro_detail = new double[N_tube, Nrow];
            double[,] href_detail = new double[N_tube, Nrow];
            double[,] mr_detail = new double[N_tube, Nrow];

            int flag_ciro = 0;
            int Ncir_forDP = 0;
            double[] mr_forDP = new double[Nciri];
            int k;
            double te_calc = 0;

            CheckAir airConverge = new CheckAir();
            CheckDP dPconverge=new CheckDP();
            CheckPri priconverge = new CheckPri();
            for (int i = 0; i < Nrow; i++) N_tube_total += Ntube[i];
            for (int i = 0; i < Nciro; i++) mr_ciro[i] = mr / Nciro;

            bool index_outbig;
            if (CircuitInfo.UnequalCir == null || CircuitInfo.UnequalCir[0] > 0)
                index_outbig = false;
            else
                index_outbig = true;

            if (CircuitInfo.UnequalCir != null)
            {
                for (int j = 0; j < Nciro; j++)
                {
                    for (int i = 0; i < Ncir; i++)
                        if (CircuitInfo.UnequalCir[i] == Nciri + 1 + j) Ngroupin[j]++;
                    //for (int i = 0; i < Nciri; i++) mr_ciri[i] = mr_ciro[j] / Ngroupin[j];
                }
            }
            int iterforAir = 0;
            int iterforDP = 0;
            int iterforPri = 0;

            double tri1 = 0;
            double te1 = 0;
            //Starting properties
            do
            {
            #region //AirConverge
            do
            {
                r = new CalcResult[Ncir];
                r1 = new CalcResult[Ncir];
                res_cir2 = new CalcResult[Nciro + 1];

                flag_ciro = (index_outbig ? 1 : 0);
                //tri = tri;
                //制冷制热模块计算切换
                if (hexType == 0)
                {
                    tri = CoolProp.PropsSI("T", "P", pri * 1000, "Q", 0, fluid) - 273.15;
                }
                else
                {
                    te = CoolProp.PropsSI("T", "P", pri * 1000, "Q", 0, fluid) - 273.15;
                }

                for (int j = 0; j < (flag_ciro == 1 ? (index_outbig ? Nciri + 1 : 1) : Nciro + 1); j++)
                {
                    if (j >= Nciro)
                    {
                        j = j - Nciro; //for Nciro
                        flag_ciro = (index_outbig ? 0 : 1);
                    }
                    if (j == 1 && index_outbig && index == 0)
                    {
                        j = j - 1;
                        flag_ciro = 0;
                    }

                    index = 0;
                    #region //DPConverge
                    do
                    {
                        k = 0;
                        if(!index_outbig)
                        { 
                            for (int i = 0; i < (flag_ciro == 1 ? Nciro : (Nciri == Nciro ? Ncir : Ncir - Nciro)); i++)
                            {
                                if (flag_ciro == 1)
                                {
                                    pri_cir[i + Ncir - Nciro] = res_cir2[i].Pro;
                                    hri_cir[i + Ncir - Nciro] = res_cir2[i].hro;
                                    tri_cir[i + Ncir - Nciro] = res_cir2[i].Tro;
                                }
                                else
                                {
                                    pri_cir[i] = pri;
                                    hri_cir[i] = hri;
                                    tri_cir[i] = tri;
                                }
                            }
                        }
                        else
                        {
                            for (int i = 0; i < (flag_ciro == 1 ? Nciro : Ncir); i++)
                            {
                                if (flag_ciro == 1)
                                {
                                    pri_cir[i] = pri;
                                    hri_cir[i] = hri;
                                    tri_cir[i] = tri;
                                }
                                else
                                {
                                    if (CircuitInfo.UnequalCir[i] == Nciri + 1 + j)
                                    {
                                        pri_cir[i] = r2[j].Pro;
                                        hri_cir[i] = r2[j].hro;
                                        tri_cir[i] = r2[j].Tro;
                                    }
                                }
                            }
                        }
                        for (int i = 0; i < Ncir; i++)
                        {
                            if (flag_ciro == 1)
                            {
                                //汇管计算
                                if(CircuitInfo.UnequalCir[i]<=0)
                                {
                                    
                                    //for (int i = 0; i < Ncir; i++)
                                    r[i] = Circuit.CircuitCalc(i, cirArr, CircuitInfo, Nrow, Ntube, Nelement, fluid, dh, l, geo.element, ta, RH,
                                        tri_cir[i], pri_cir[i], hri_cir[i], mr_ciro[k], ma, ha, eta_surface, zh, zdp, hexType, thickness, conductivity, Pwater,Airdirection);
                                    if (r[i].Pro < 0) { res_slab.Pro = -10000000; return res_slab; }
                                    r1[k] = r[i].ShallowCopy();
                                    r2[k] = r[i].ShallowCopy();
                                    if (!index_outbig) r1[k].DP += res_cir2[k].DP;
                                  
                                    index_cir[k] = i;
                                    k++;
                                    Ncir_forDP = Nciro;
                                    mr_forDP = (double[])mr_ciro.Clone(); // mr_forDP = mr_ciro
                                    if (k == Nciro) break;
                                }
                            }
                            else if (Nciri == Nciro || CircuitInfo.UnequalCir[i] == Nciri + 1 + j)
                            {
                                //均匀流路计算和不均匀流路开始部分（独立管）计算
                                if (index == 0)
                                {
                                    if (Nciri == Nciro)
                                    {
                                        mr_ciro.CopyTo(mr_ciri, 0);
                                    }
                                    else
                                    {
                                        if (restartDP_index == 1 || !priconverge.flag)
                                        {
                                            var mm = mr_ciri_base[j].Sum();
                                            //foreach (var item in mr_ciri_base[j])
                                            //mm += item; 
                                            mr_ciri[k] = mr_ciri_base[j][k] * mr_ciro[j] / mm;//(mr / Nciro);
                                        }
                                        else
                                            mr_ciri[k] = mr_ciro[j] / Ngroupin[j];
                                    }
                                }
                                //else mr_ciri_base.CopyTo(mr_ciri[k], 0);

                                //for (int i = 0; i < Ncir; i++)

                                //首次流路计算

                                r[i] = Circuit.CircuitCalc(i, cirArr, CircuitInfo, Nrow, Ntube, Nelement, fluid, dh, l, geo.element, ta, RH,
                                    tri_cir[i], pri_cir[i], hri_cir[i], mr_ciri[k], ma, ha, eta_surface, zh, zdp, hexType, thickness, conductivity, Pwater,Airdirection);
                                if (r[i].Pro < 0) { res_slab.Pro = -10000000; return res_slab; }
                                r1[k] = r[i].ShallowCopy();
                                index_cir[k] = i;//不均匀流路的输出才会用到
                                k++;

                                if (k == (Nciri == Nciro ? Ncir : Ngroupin[j]))
                                {
                                    Ncir_forDP = (Nciri == Nciro ? Ncir : Ngroupin[j]);
                                    mr_forDP = (double[])mr_ciri.Clone();
                                    break;
                                }
                            }
                        }

                        if (index_outbig && flag_ciro == 1) break;

                        index++;
                        //dPconverge = CheckDPforCircuits.CheckDPConverge(mr, mr_ciri, r, Ncir);
                        dPconverge = CheckDPforCircuits.CheckDPConverge(res_cir2, iterforPri, flag_ciro, mr_forDP, r1, Ncir_forDP);

                        if (flag_ciro == 0)
                        {
                            restartDP_index = 0;
                            if (!dPconverge.flag) dPconverge.mr.CopyTo(mr_ciri, 0);//mr_ciri = dPconverge.mr;   
                        }
                        else //(flag_ciro == 1)
                        {
                            if (dPconverge.flag)
                            {
                                restartDP_index = 0;
                            }
                            else
                            {
                                restartDP_index = 1;
                                dPconverge.mr.CopyTo(mr_ciro, 0); //mr_ciro = dPconverge.mr;
                                break;
                            }
                        }
                        iterforDP++;
                        

                        N_tube2 = 0;
                        #region //Result print out
                        if (dPconverge.flag)
                        {
                            if (Nciri == Nciro) 
                                //te_calc = Refrigerant.SATP(fluid, composition, r[j].Pro, 1).Temperature;
                                te_calc = CoolProp.PropsSI("T", "P", r[j].Pro * 1000, "Q", 0, fluid);
                            else
                            {                            
                                if (mr_ciri_base.Count == Nciro && flag_ciro == 0)
                                {
                                    mr_ciri_base.RemoveAt(index_mr_ciri_base);
                                    mr_ciri_base.Insert(index_mr_ciri_base, mr_forDP);
                                    index_mr_ciri_base++;
                                    index_mr_ciri_base %= mr_ciri_base.Count;
                                }                                
                                if (mr_ciri_base.Count < Nciro) mr_ciri_base.Add(mr_forDP); //keep original mr ratio for fast iter
                                j = (flag_ciro == 1 ? j + Nciro : j);
                                res_cir2[j] = new CalcResult();
                                for (int i = 0; i < (flag_ciro == 1 ? Nciro : Ngroupin[j]); i++)
                                {
                                    res_cir2[j].Q += r1[i].Q;
                                    res_cir2[j].M += r1[i].M;
                                    res_cir2[j].hro += (flag_ciro == 1 ? mr_ciro[i] : mr_ciri[i]) * r1[i].hro;
                                    if (fluid == "Water")
                                        res_cir2[j].Tro += (flag_ciro == 1 ? mr_ciro[i] : mr_ciri[i]) * r1[i].Tro;

                                    res_cir2[j].Vel_r = r1[i].Vel_r;
                                    res_cir2[j].href += r1[i].href * CircuitInfo.TubeofCir[index_cir[i]];
                                    res_cir2[j].R_1 += r1[i].R_1 * CircuitInfo.TubeofCir[index_cir[i]];
                                    res_cir2[j].R_1a += r1[i].R_1a * CircuitInfo.TubeofCir[index_cir[i]];
                                    res_cir2[j].R_1r += r1[i].R_1r * CircuitInfo.TubeofCir[index_cir[i]];

                                    N_tube2 += CircuitInfo.TubeofCir[index_cir[i]];

                                }
                                res_cir2[j].DP = r1[(flag_ciro == 1 ? Nciro : Ngroupin[j]) - 1].DP;
                                res_cir2[j].Tao_Detail = ta;
                                res_cir2[j].Pro = r1[(flag_ciro == 1 ? Nciro : Ngroupin[j]) - 1].Pro;
                                res_cir2[j].hro = res_cir2[j].hro / (flag_ciro == 1 ? mr : mr_ciro[j]);

                                res_cir2[j].href = res_cir2[j].href / N_tube2;
                                res_cir2[j].R_1 = res_cir2[j].R_1 / N_tube2;
                                res_cir2[j].R_1a = res_cir2[j].R_1a / N_tube2;
                                res_cir2[j].R_1r = res_cir2[j].R_1r / N_tube2;
                                res_cir2[j].Tri = tri;

                                //te_calc = Refrigerant.SATP(fluid, composition, res_cir2[j].Pro, 1).Temperature;
                                te_calc = CoolProp.PropsSI("T", "P", res_cir2[j].Pro * 1000, "Q", 0, fluid);

                                if (fluid == "Water")
                                    res_cir2[j].Tro = res_cir2[j].Tro / (flag_ciro == 1 ? mr : mr_ciro[j]);
                                else
                                    res_cir2[j].Tro = CoolProp.PropsSI("T", "P", res_cir2[j].Pro * 1000, "H", res_cir2[j].hro * 1000, fluid);
                            }

                        }
                        #endregion
                    #endregion
                    } while (!dPconverge.flag && iterforDP < 100);

                    if (Nciri == Nciro) break;

                    if (index_outbig && j == Nciro - 1)
                    {
                        for (int i = 0; i < Nciro; i++)
                        {
                            r2[i].DP += res_cir2[i].DP;
                        }                       
                        flag_ciro = 1;
                        Ncir_forDP = Nciro;
                        mr_forDP = (double[])mr_ciro.Clone(); // mr_forDP = mr_ciro
                        dPconverge = CheckDPforCircuits.CheckDPConverge(res_cir2, iterforPri, flag_ciro, mr_forDP, r2, Ncir_forDP);
                        if (!dPconverge.flag)
                        {
                            restartDP_index = 1;
                            dPconverge.mr.CopyTo(mr_ciro, 0); //mr_ciro = dPconverge.mr;
                        }
                        break;
                    }
                }
                if (Airdirection == "Parallel")
                    airConverge.flag = true;
                else//Counter
                {
                    airConverge = CheckAirConvergeforCircuits.CheckAirConverge(cirArrforAir.TotalDirection, Nrow, N_tube, Nelement, ta, RH, r); //taout_calc, RHout_calc
                    ta = airConverge.ta;
                    RH = airConverge.RH;
                    iterforAir++;
                }
                //Add Q converge criterion to avoid results oscillation, ruhao 20180426
                if (Airdirection != "Parallel") //No airConverge iter for Parallel
                {
                    for (int i = 0; i < Ncir; i++) Q1[iterforAir-1] += r[i].Q;

                    try
                    {
                        if ( Q1[iterforAir-1] < Q1[iterforAir - 2] && (Math.Abs(Q1[iterforAir-1] - Q1[iterforAir - 3]) / Q1[iterforAir-1] < 0.0001)||
                            Math.Abs(Q1[iterforAir - 1] - Q1[iterforAir - 4]) / Q1[iterforAir - 1] < 0.0001)
                        {
                            airConverge.flag = true;
                        }
                    }
                    catch(Exception ex)
                    {
                        continue;
                    }

                }
            } while (!airConverge.flag && iterforAir < 50);
            
            #endregion
                //using (StreamWriter wr = File.AppendText(@"D:\Work\Simulation\Test\MinNout.txt"))
                //{
                    //for (int i = 0; i < Ncir; i++)
                    //{
                        //wr.WriteLine("Q, {0}, DP, {1}, href, {2}, Ra_ratio, {3}, Tao, {4}, Tro, {5}, mr, {6}", r[i].Q, r[i].DP, r[i].href, r[i].Ra_ratio, r[i].Tao, r[i].Tro, r[i].mr);
                    //}
                //}
            if (restartDP_index == 1)
                priconverge.flag = false;
            else if (hexType == 0 && (fluid != "Water"))
            {
                priconverge = CheckPin.CheckPriConverge(te, te_calc - 273.15, pri, pe, r[Ncir - 1].Pro); //res_slab.Pro
                iterforPri++;
                pri = priconverge.pri;
                if (priconverge.flag && iterforPri == 1 && iterforDP == 1)
                    priconverge.flag = false; //to avoid not even iterate but converge by chance 
            }
            else
                priconverge.flag = true;

            } while (!priconverge.flag && iterforPri < 50);


            if (iterforDP >= 100)
            {
                return res_slab;
                throw new Exception("iter for DPConverge > 100.");
            }
            if (iterforPri >= 50)
            {
                return res_slab;
                throw new Exception("iter for Pri > 50.");
            }

            #region //Result print out

            for (int i = 0; i < Ncir; i++)
            {
                res_slab.Q += r[i].Q;
                res_slab.M += r[i].M;
                if (Nciri == Nciro) res_slab.hro += mr_ciri[i] * r[i].hro;
                res_slab.href += r[i].href * CircuitInfo.TubeofCir[i];
                res_slab.R_1 += r[i].R_1 * CircuitInfo.TubeofCir[i];
                res_slab.R_1a += r[i].R_1a * CircuitInfo.TubeofCir[i];
                res_slab.R_1r += r[i].R_1r * CircuitInfo.TubeofCir[i];
                for(int j=0;j<N_tube;j++)//detail output
                {
                    for(k=0;k<Nrow;k++)
                    {
                        if (r[i].Q_detail[j, k] != 0) Q_detail[j, k] = r[i].Q_detail[j, k];
                        if (r[i].DP_detail[j, k] != 0) DP_detail[j, k] = r[i].DP_detail[j,k];
                        if (r[i].Tri_detail[j, k] != 0) Tri_detail[j, k] = r[i].Tri_detail[j, k];
                        if (r[i].Pri_detail[j, k] != 0) Pri_detail[j, k] = r[i].Pri_detail[j, k];
                        if (r[i].hri_detail[j, k] != 0) hri_detail[j, k] = r[i].hri_detail[j, k];
                        if (r[i].Tro_detail[j, k] != 0) Tro_detail[j, k] = r[i].Tro_detail[j, k];
                        if (r[i].Pro_detail[j, k] != 0) Pro_detail[j, k] = r[i].Pro_detail[j, k];//
                        if (r[i].hro_detail[j, k] != 0) hro_detail[j, k] = r[i].hro_detail[j, k];
                        if (r[i].href_detail[j, k] != 0) href_detail[j, k] = r[i].href_detail[j, k];
                        if (r[i].mr_detail[j, k] != 0) mr_detail[j, k] = r[i].mr_detail[j, k];
                    }
                }
            }
            if (Nciri == Nciro)
            {
                res_slab.hro = res_slab.hro / mr;
                res_slab.Pro = r[Ncir - 1].Pro;
                res_slab.Vel_r = r[Ncir - 1].Vel_r;
            }
            else if(!index_outbig)
            { 
                res_slab.hro = res_cir2[Nciro].hro;
                res_slab.Pro = res_cir2[Nciro].Pro;
                res_slab.Vel_r = res_cir2[Nciro].Vel_r;
            }
            else
            {
                res_slab.hro = res_cir2[Nciro - 1].hro;
                res_slab.Pro = res_cir2[Nciro - 1].Pro;
                res_slab.Vel_r = res_cir2[Nciro - 1].Vel_r;
            }
            res_slab.Pri = pri;
            res_slab.Tri = tri;
            res_slab.hri = hri;
            res_slab.mr = mr;
            res_slab.DP = pri - res_slab.Pro;
            res_slab.Tao_Detail = ta;
            res_slab.RHo_Detail = RH;
            res_slab.href = res_slab.href / N_tube_total;
            for (int i = 0; i < ha.GetLength(0); i++)
                for (int j = 0; j < ha.GetLength(1); j++)
                {
                    res_slab.ha += ha[i, j];
                }
            res_slab.ha = res_slab.ha / ha.Length;
            res_slab.R_1 = res_slab.R_1 / N_tube_total;
            res_slab.R_1a = res_slab.R_1a / N_tube_total;
            res_slab.R_1r = res_slab.R_1r / N_tube_total;


            te_calc = CoolProp.PropsSI("T", "P", res_slab.Pro * 1000, "Q", 0, fluid) - 273.15;
            double densityLo = CoolProp.PropsSI("D", "T", te_calc + 273.15, "Q", 0, fluid);
            double densityVo = CoolProp.PropsSI("D", "T", te_calc + 273.15, "Q", 1, fluid);
            double hlo = CoolProp.PropsSI("H", "T", te_calc + 273.15, "D", densityLo, fluid) / 1000 ;
            double hvo = CoolProp.PropsSI("H", "T", te_calc + 273.15, "D", densityVo, fluid) / 1000 ;
            res_slab.x_o = (res_slab.hro - hlo) / (hvo - hlo);

            double hli = CoolProp.PropsSI("H", "P", pri * 1000, "Q", 0, fluid) / 1000 ;
            double hvi = CoolProp.PropsSI("H", "P", pri * 1000, "Q", 1, fluid) / 1000 ;

            res_slab.x_i = (res_slab.hri - hli) / (hvi - hli);
            res_slab.Tro = CoolProp.PropsSI("T", "P", res_slab.Pro * 1000, "H", res_slab.hro  * 1000, fluid) - 273.15;

            double h = res_slab.hro;
            for (int j = 0; j < N_tube; j++)
                for (int i = 0; i < Nelement; i++)
                {
                    res_slab.Tao += res_slab.Tao_Detail[i, j, Nrow];
                    res_slab.RHout += res_slab.RHo_Detail[i, j, Nrow];
                }

            res_slab.Tao = res_slab.Tao / (N_tube * Nelement);
            res_slab.RHout = res_slab.RHout / (N_tube * Nelement);
            res_slab.Ra_ratio = res_slab.R_1a / res_slab.R_1;
            for (int i = 0; i < ma.GetLength(0); i++)
                for (int j = 0; j < ma.GetLength(1); j++)
                {
                    res_slab.ma += ma[i, j];
                }
            res_slab.Va = res_slab.ma / 1.2 * 3600;
            res_slab.Q_detail = Q_detail;//detail output
            res_slab.DP_detail = DP_detail;
            res_slab.Tri_detail = Tri_detail;
            res_slab.Pri_detail = Pri_detail;
            res_slab.hri_detail = hri_detail;
            res_slab.Tro_detail = Tro_detail;
            res_slab.Pro_detail = Pro_detail;
            res_slab.hro_detail = hro_detail;
            res_slab.href_detail = href_detail;
            res_slab.mr_detail = mr_detail;
            res_slab.Aa = geo.total.A_a;
            res_slab.Ar = geo.total.A_r;
            res_slab.AHx = geo.total.A_hx;
            res_slab.N_row = Nrow;
            res_slab.tube_row = N_tube;

            return res_slab;
            #endregion
        }
        
    }
}
