using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace mc3vray
{
    public class Ray
    {
        public static List<double> Hz = new List<double>(); // вузлові точки глибин
        public static List<double> Cz = new List<double>(); // швидкість звуку по вузловим точкам глибин

        public static double[] Kz;                          // водні прошарки відповідно вузлових точок швидкості звуку
        public static double[] Yr;                          // ординати центрів кіл для кожного водного шару

        public static double Hgas;                          // глибина 

        public static double Ksrf = 0.9;                    // коефіціент ослаблення при відбитті від поверхні моря
        public static double Kbtm = 0.7;                    // коефіціент ослаблення при відбитті від дна моря
        public static double Kenv = 0.001;                  // ослаблення амплітуди променів від пройденої відстані на 1 км

        // 

        public void BuildRay(double lobj, double Tetgr, int n, out double lr,
            out double tr, out double amp, out double bFi, out double eFi, out double rFi, out double xFi, out double yFi, out int iFi)
        {
            #region початкові налаштування

            lr = 0;             // довжина траєкторії променя
            tr = 0;             // час руху променя
            amp = 1;
            bFi = 0;
            eFi = 0; iFi = 0;
            rFi = 0; xFi = 0; yFi = 0;

            int ig = 0;          // номер водного шару в якому знаходиться джерело звуку


            // якщо початковий кут променя >90°
            if (Tetgr > 90)      
            {
                for (int i = 0; i < Kz.Length; i++)
                {
                    if (Hgas > Hz[i] && Hgas <= Hz[i + 1])   // знаходимо номер водного шару, в якому знаходиться джерело звуку
                    {
                        ig = i;
                        break;
                    }
                }
            }
            // якщо початковий кут променя <=90°
            else
            {
                for (int i = 0; i < Kz.Length; i++)
                {   // якщо початковий кут =90° і джерело звуку знаходиться не на дні
                    if (Tetgr == 90 && Hgas == Hz[i]) Hgas += 0.0001;              // то джерело відноситься до наступного водного шару
                    if (Tetgr == 90 && Hgas == Hz[Kz.Length]) Hgas -= 0.0001;       // якщо джерело на дні, то до попереднього
                    if (Hgas >= Hz[i] && Hgas < Hz[i + 1])   // для цього випадку теж знаходимо номер водного шару, в якому знаходиться джерело звуку
                    {
                        ig = i;
                        break;
                    }
                }
            }


            bool inobj = false;
            double cg = Cz[ig] + Kz[ig] * (Hgas - Hz[ig]);          // швидкість звуку джерела
            double Tet = Tetgr * Math.PI / 180;                     // початковий кут траєкторії променя переводимо з градусною міри в радіани
            double rg = cg / (Math.Abs(Kz[ig]) * Math.Sin(Tet));    // обчислюємо радіус кола для водного шару, в якому знаходиться джерело звуку
            double yrg = cg / Kz[ig] - Hgas;                        // обчислюємо ординату центра кола для водного шару, в якому знаходиться джерело звуку
            double xrg = cg / (Math.Tan(Tet) * Kz[ig]);             // обчислюємо абсцису центра кола для водного шару, в якому знаходиться джерело звуку
            double[] fi_begin = new double[Kz.Length];              // масив початкових кутів для параметричного рівняння кола для кожного водного шару
            double[] fi_end = new double[Kz.Length];                // масив кінцевих кутів для параметричного рівняння кола для кожного водного шару
            bool mkb90b = false;                                    // логічна змінна означає, що направлений вгору промінь пройшов крізь водний шар
            bool bkm90b = false;                                    // логічна змінна означає, що направлений вниз промінь пройшов крізь водний шар
            double[] xr = new double[Kz.Length];                    // масив абсцис центрів кіл для кожного водного шару
            double[] r = new double[Kz.Length];                     // масив радіусів кіл для кожного водного шару
            double[] xzdv = new double[Kz.Length];                  // масив останньої абсциси на проміжках, що відповідають водним шарам

            Yr[ig] = yrg;                                           // спочатку присвоюємо ординату центра кола для водного шару, в якому знаходиться джерело звуку
            r[ig] = rg;                                             // присвоюємо елементу масиву радіус кола для водного шару, в якому знаходиться джерело звуку
            xr[ig] = xrg;                                           // присвоюємо абсцису центра кола для водного шару, в якому знаходиться джерело звуку
            double fi_endg;                                         // початковий кут для параметричного рівняння кола для поточного водного шару
            double fi_beging;                                       // кінцевий кут для параметричного рівняння кола для поточного водного шару

            #endregion

            #region джерело між шарами

            if (Kz[ig] < 0)                                         // якщо коефіцієнт зміни швидкості звуку для водного шару, в якому знаходиться
            {                                                       // джерело звуку менше нуля, тобто швидкість звуку спадає, промінь прогинається вгору
                if (Tetgr <= 90)                                    // якщо початковий кут променя не більший, ніж 90°, тобто промінь направлений вниз або прямо
                {
                    fi_endg = Tet;                                  // кінцевим кутом дуги кола є початковий кут траєкторії променя
                    fi_beging = Math.Asin(Math.Abs(yrg + Hz[ig + 1]) / rg);  // за допомогою синуса знаходимо початковий кут дуги кола

                    // HACH: if (ig + 1 == Kz.Length) amp *= Kbtm;
                }
                else                                                // якщо початковий кут більше 90°, тобто промінь направлений вгору
                {
                    fi_endg = Tet;                                  // кінцевим кутом дуги кола є початковий кут траєкторії променя
                    if (Math.Abs(yrg + Hz[ig]) < rg)                // якщо радіус кола більший, ніж верхня межа водного шару
                    {                                               // початковий кут дуги кола

                        fi_begin[ig] = Math.Asin(Math.Abs(yrg + Hz[ig]) / rg);  // зберігаємо початковий кут для поточного водного шару
                        fi_beging = Math.PI - fi_begin[ig];
                        mkb90b = true;                              // промінь пройшов крізь водний шар

                        // HACH: if (ig == 0) amp *= Ksrf;
                    }
                    else                                            // якщо радіус кола не більший, ніж верхня межа водного шару, то промінь не пройшов крізь водний шар
                    {                                               // а заломився в ньому та пішов вниз, зберігаємо початковий кут дуги кола як кінцевий кут для поточного водного шару
                        fi_beging = Math.Asin(Math.Abs(yrg + Hz[ig + 1]) / rg);
                        fi_end[ig] = fi_beging;

                        // HACH: if (ig + 1 == Kz.Length) amp *= Kbtm;
                    }
                }

                xzdv[ig] = xrg + rg * Math.Cos(fi_beging);
                if (xrg + xzdv[ig] > lobj && xrg + rg * Math.Cos(fi_endg) <= lobj)
                {
                    eFi = fi_endg;
                    bFi = fi_beging;
                    rFi = rg; xFi = xrg; yFi = yrg; iFi = ig;
                    inobj = true;
                }
                else if (xzdv[ig] <= lobj)
                {
                    double lri = Math.Abs(rg * (fi_endg - fi_beging));
                    tr += 2 * lri / (Cz[ig + 1] + Cz[ig]);
                    lr += lri;
                }
            }
            else                                                    // якщо коефіцієнт зміни швидкості звуку для водного шару, в якому знаходиться джерело звуку
            {                                                       // не менший, ніж нуль, тобто швидкість звуку зростає, промінь прогинається вниз
                if (Tetgr <= 90)                                    // початковий кут не більший, ніж 90°, тобто промінь направлений вниз або прямо
                {
                    fi_beging = Math.PI + Tet;                      // початковий кут дуги кола буде більшим, ніж 180°
                    if (Math.Abs(yrg + Hz[ig + 1]) <= rg)           // якщо радіус кола не менший, ніж нижня межа водного шару
                    {                                               // зберігаємо кінцевий кут дуги кола, як кінцевий кут для водного шару
                        fi_end[ig] = Math.Asin(Math.Abs(yrg + Hz[ig + 1]) / rg);
                        fi_endg = Math.PI + fi_end[ig];
                        bkm90b = true;                              // промінь пройшов крізь водний шар

                        // HACH: if (ig + 1 == Kz.Length) amp *= Kbtm;
                    }
                    else                                            // якщо радіус кола менший, ніж нижня межа водного шару, то промінь не пройде крізь водний шар
                    {                                               // а заломиться в ньому та піде вгору, зберігаємо кінцевий кут дуги кола, як початковий кут для водного шару
                        fi_begin[ig] = Math.Asin(Math.Abs(yrg + Hz[ig]) / rg);
                        fi_endg = 2 * Math.PI - fi_begin[ig];
                        
                        // HACH: if (ig == 0) amp *= Ksrf;
                    }
                }
                else                                                // якщо початковий кут більший, ніж 90°, тобто промінь направлений вгору
                {
                    fi_beging = Math.PI + Tet;                      // початковий кут траєкторії буде більшим, ніж 270°

                    fi_begin[ig] = Math.Asin(Math.Abs(yrg + Hz[ig]) / rg);
                    fi_endg = 2 * Math.PI - fi_begin[ig];
                    
                    // HACH: if (ig == 0) amp *= Ksrf;
                }

                xzdv[ig] = xrg + rg * Math.Cos(fi_endg);
                if (xzdv[ig] > lobj && xrg + rg * Math.Cos(fi_beging) <= lobj)
                {
                    eFi = fi_endg;
                    bFi = fi_beging;
                    rFi = rg; xFi = xrg; yFi = yrg; iFi = ig;
                    inobj = true;
                }
                else if (xzdv[ig] <= lobj)
                {
                    double lri = Math.Abs(rg * (fi_endg - fi_beging));
                    tr += 2 * lri / (Cz[ig + 1] + Cz[ig]);
                    lr += lri;
                }
            }                                                       // початок траєкторії у водному шарі, де розташоване джерело звуку, побудовано

            #endregion

            #region Цикл 1

            bool nedif = false;
            for (int ii = 0; ii < n; ii++)                  // цикл повторюється доки не побудується задана кількість періодів променя
            {                                               // якщо промінь направлений вгору і пройшов крізь водний шар; або був направлений вниз, заломився у водному шарі і пішов вгору
                if (Tetgr > 90 && (mkb90b || Kz[ig] >= 0) || Tetgr <= 90 && !(bkm90b || Kz[ig] < 0))
                {
                    bool ned = false;                       // чи заломився промінь у водному шарі та змінив напрям на протилежний 
                    int ined = 0;                           // номер водного шару де заломився промінь
                    for (int i = ig - 1; i >= 0; i--)       // цикл для побудови траєкторії променя вгору
                    {
                        if (ii > 0 && !nedif)
                            break;
                        if (Kz[i] < 0)                      // поточний коефіцієнт зміни швидкості звуку за глибиною від'ємний
                        {                                   // промінь вигинається вгору
                            fi_endg = Math.PI - fi_begin[i + 1];
                            r[i] = Cz[i + 1] / (Math.Abs(Kz[i]) * Math.Sin(fi_begin[i + 1]));
                            xr[i] = Cz[i + 1] / (Kz[i] * Math.Tan(fi_endg)) + xzdv[i + 1];

                            if (Math.Abs(Yr[i] + Hz[i]) < Math.Abs(r[i]))  // якщо радіус кола більший за верхню межу поточного водного шару
                            {                               // промінь проходить крізь водний шар
                                fi_begin[i] = Math.Asin(Math.Abs(Yr[i] + Hz[i]) / r[i]);
                                fi_beging = Math.PI - fi_begin[i];
                            }
                            else                            // якщо радіус кола не більший за верхню межу поточного водного шару, то промінь не проходить крізь водний шар
                            {                               // а заломлюється в ньому та прямує вниз, початковий кут дуги кола зберігаємо, як кінцевий кут для водного шару
                                fi_beging = Math.Asin(Math.Abs(Yr[i] + Hz[i + 1]) / r[i]);
                                fi_end[i] = fi_beging;
                                ned = true;                 // промінь не пройшов крізь водний шар, а змінив напрям на протилежний
                                ined = i;                   // зберігаємо номер водного шару крізь який не зміг пройти промінь
                            }

                            xzdv[i] = xr[i] + r[i] * Math.Cos(fi_beging);
                            if (xzdv[i] > lobj && xr[i] + r[i] * Math.Cos(fi_endg) <= lobj)
                            {
                                eFi = fi_endg;
                                bFi = fi_beging;
                                rFi = r[i]; xFi = xr[i]; yFi = Yr[i]; iFi = i;
                                inobj = true;
                            }
                            else if (xzdv[i] <= lobj)
                            {
                                double lri = Math.Abs(r[i] * (fi_endg - fi_beging));
                                tr += 2 * lri / (Cz[i + 1] + Cz[i]);
                                lr += lri;
                            }
                            
                            if (ned) break;                 // якщо промінь заломився та змінив напрям на протилежний, то виходимо з циклу
                        }
                        else                                // якщо коефіцієнт зміни швидкості звуку за глибиною не від'ємний, промінь вгинається вниз
                        {
                            fi_beging = 2 * Math.PI - fi_begin[i + 1];
                            r[i] = Cz[i + 1] / (Math.Abs(Kz[i]) * Math.Sin(fi_begin[i + 1]));
                            xr[i] = Cz[i + 1] / (Kz[i] * Math.Tan(fi_beging)) + xzdv[i + 1];
                            fi_endg = 2 * Math.PI - Math.Asin(Math.Abs(Yr[i] + Hz[i]) / r[i]);

                            xzdv[i] = xr[i] + r[i] * Math.Cos(fi_endg);
                            if (xzdv[i] > lobj && xr[i] + r[i] * Math.Cos(fi_beging) <= lobj)
                            {
                                eFi = fi_endg;
                                bFi = fi_beging;
                                rFi = r[i]; xFi = xr[i]; yFi = Yr[i]; iFi = i;
                                inobj = true;
                            }
                            else if (xzdv[i] <= lobj)
                            {
                                double lri = Math.Abs(r[i] * (fi_endg - fi_beging));
                                tr += 2 * lri / (Cz[i + 1] + Cz[i]);
                                lr += lri;
                            }

                            fi_begin[i] = Math.Asin(Math.Abs(Yr[i] + Hz[i]) / r[i]);
                        }
                    }

                    nedif = false;
                    if (ned)                                // якщо промінь заломився та змінив напрям згори вниз
                    {
                        #region
                        for (int i = ined + 1; i < Kz.Length; i++)    // цикл проходить вниз по водних шарах
                        {
                            if (Kz[i] < 0)                  // коефіцієнт зміни швидкості звуку за глибиною від'ємний
                            {                               // промінь вигинається вгору
                                fi_endg = fi_end[i - 1];
                                r[i] = Cz[i] / (Math.Abs(Kz[i]) * Math.Sin(fi_end[i - 1]));
                                xr[i] = Cz[i] / (Kz[i] * Math.Tan(fi_endg)) + xzdv[i - 1];
                                fi_beging = Math.Asin(Math.Abs(Yr[i] + Hz[i + 1]) / r[i]);

                                xzdv[i] = (float)(xr[i] + r[i] * Math.Cos(fi_beging));
                                if (xzdv[i] > lobj && xr[i] + r[i] * Math.Cos(fi_endg) <= lobj)
                                {
                                    eFi = fi_endg;
                                    bFi = fi_beging;
                                    rFi = r[i]; xFi = xr[i]; yFi = Yr[i]; iFi = i;
                                    inobj = true;
                                }
                                else if (xzdv[i] <= lobj)
                                {
                                    double lri = Math.Abs(r[i] * (fi_endg - fi_beging));
                                    tr += 2 * lri / (Cz[i + 1] + Cz[i]);
                                    lr += lri;
                                }

                                fi_end[i] = fi_beging;
                            }
                            else                            // коефіцієнт зміни швидкості звуку не менше нуля, промінь вигинається вниз
                            {                               // в даному випадку можлива ситуація, коли промінь знову заломиться у водному шарі
                                ned = false;                // та змінить напрям знизу вгору
                                fi_beging = Math.PI + fi_end[i - 1];
                                r[i] = Cz[i] / (Math.Abs(Kz[i]) * Math.Sin(fi_end[i - 1]));
                                xr[i] = Cz[i] / (Kz[i] * Math.Tan(fi_beging)) + xzdv[i - 1];

                                if (Math.Abs(Yr[i] + Hz[i + 1]) <= Math.Abs(r[i]))   // якщо радіус кола не менший за нижню межу водного шару
                                {                           // промінь пройшов крізь водний шар
                                    fi_end[i] = Math.Asin(Math.Abs(Yr[i] + Hz[i + 1]) / r[i]);
                                    fi_endg = Math.PI + fi_end[i];
                                }
                                else                        // якщо радіус кола менший за нижню межу водного шару, то промінь не проходить крізь водний шар
                                {                           // а заломлюється в ньому та змінює напрям знизу вгору
                                    fi_begin[i] = Math.Asin(Math.Abs(Yr[i] + Hz[i]) / r[i]);
                                    fi_endg = 2 * Math.PI - fi_begin[i];
                                    
                                    ned = true;             // промінь змінив напрям на протилежний
                                    ined = i;               // зберігаємо номер водного шару, в якому промінь змінив напрям знизу вгору
                                }

                                xzdv[i] = xr[i] + r[i] * Math.Cos(fi_endg);
                                if (xzdv[i] > lobj && xr[i] + r[i] * Math.Cos(fi_beging) <= lobj)
                                {
                                    eFi = fi_endg;
                                    bFi = fi_beging;
                                    rFi = r[i]; xFi = xr[i]; yFi = Yr[i]; iFi = i;
                                    inobj = true;
                                }
                                else if (xzdv[i] <= lobj)
                                {
                                    double lri = Math.Abs(r[i] * (fi_endg - fi_beging));
                                    tr += 2 * lri / (Cz[i + 1] + Cz[i]);
                                    lr += lri;
                                }
                                
                                if (ned) break;             // якщо промінь заломився та змінив напрям на протилежний, то виходимо з циклу
                            }
                        }                                   // кінець циклу, який проходить вниз по водних шарах

                        if (ned)
                        {
                            for (int i = ined - 1; i >= ig; i--)
                            {
                                if (Kz[i] >= 0)
                                {
                                    fi_beging = 2 * Math.PI - fi_begin[i + 1];
                                    r[i] = Cz[i + 1] / (Math.Abs(Kz[i]) * Math.Sin(fi_begin[i + 1]));
                                    xr[i] = Cz[i + 1] / (Kz[i] * Math.Tan(fi_beging)) + xzdv[i + 1];
                                    fi_endg = 2 * Math.PI - Math.Asin(Math.Abs(Yr[i] + Hz[i]) / r[i]);

                                    xzdv[i] = xr[i] + r[i] * Math.Cos(fi_endg);
                                    if (xzdv[i] > lobj && xr[i] + r[i] * Math.Cos(fi_beging) <= lobj)
                                    {
                                        eFi = fi_endg;
                                        bFi = fi_beging;
                                        rFi = r[i]; xFi = xr[i]; yFi = Yr[i]; iFi = i;
                                        inobj = true;
                                    }
                                    else if (xzdv[i] <= lobj)
                                    {
                                        double lri = Math.Abs(r[i] * (fi_endg - fi_beging));
                                        tr += 2 * lri / (Cz[i + 1] + Cz[i]);
                                        lr += lri;
                                    }

                                    fi_begin[i] = Math.Asin(Math.Abs(Yr[i] + Hz[i]) / r[i]);
                                }
                                else
                                {
                                    ned = false;
                                    fi_endg = Math.PI - fi_begin[i + 1];
                                    r[i] = Cz[i + 1] / (Math.Abs(Kz[i]) * Math.Sin(fi_begin[i + 1]));
                                    xr[i] = Cz[i + 1] / (Kz[i] * Math.Tan(fi_endg)) + xzdv[i + 1];

                                    if (Math.Abs(Yr[i] + Hz[i]) < Math.Abs(r[i]))
                                    {
                                        fi_begin[i] = Math.Asin(Math.Abs(Yr[i] + Hz[i]) / r[i]);
                                        fi_beging = Math.PI - fi_begin[i];
                                    }
                                    else
                                    {
                                        fi_beging = Math.Asin(Math.Abs(Yr[i] + Hz[i + 1]) / r[i]);
                                        fi_end[i] = fi_beging;

                                        ned = true;
                                        ined = i;
                                    }

                                    xzdv[i] = xr[i] + r[i] * Math.Cos(fi_beging);
                                    if (xzdv[i] > lobj && xr[i] + r[i] * Math.Cos(fi_endg) <= lobj)
                                    {
                                        eFi = fi_endg;
                                        bFi = fi_beging;
                                        rFi = r[i]; xFi = xr[i]; yFi = Yr[i]; iFi = i;
                                        inobj = true;
                                    }
                                    else if (xzdv[i] <= lobj)
                                    {
                                        double lri = Math.Abs(r[i] * (fi_endg - fi_beging));
                                        tr += 2 * lri / (Cz[i + 1] + Cz[i]);
                                        lr += lri;
                                    }
                                   
                                    if (ned) break;
                                }
                            }
                        }

                        ned = false;                        // очищаємо змінну, яка відповідає за зміну напряму променя
                        for (int i = Kz.Length - 1; i > ined + 1 && Math.Abs(ined - ig) > 1 || i > ined && !(Math.Abs(ined - ig) > 1); i--)
                        {                               // зворотній хід променя, для випадку, коли промінь змінив напрям на протилежний
                            if (Kz[i] < 0)
                            {
                                if (i == Kz.Length - 1)
                                {
                                    fi_endg = Math.PI - fi_end[i];
                                    r[i] = Cz[i + 1] / (Math.Abs(Kz[i]) * Math.Sin(fi_end[i]));
                                    xr[i] = Cz[i + 1] / (Kz[i] * Math.Tan(fi_endg)) + xzdv[i];

                                    if (!inobj) amp *= Kbtm;
                                }
                                else
                                {
                                    fi_endg = Math.PI - fi_begin[i + 1];
                                    r[i] = Cz[i + 1] / (Math.Abs(Kz[i]) * Math.Sin(fi_begin[i + 1]));
                                    xr[i] = Cz[i + 1] / (Kz[i] * Math.Tan(fi_endg)) + xzdv[i + 1];
                                }
                                if (Math.Abs(Yr[i] + Hz[i]) < Math.Abs(r[i]))
                                {
                                    fi_begin[i] = Math.Asin(Math.Abs(Yr[i] + Hz[i]) / r[i]);
                                    fi_beging = Math.PI - fi_begin[i];

                                    if (fi_endg == fi_beging) break;
                                }
                                else
                                {
                                    fi_beging = Math.Asin(Math.Abs(Yr[i] + Hz[i + 1]) / r[i]);

                                    if (fi_endg == fi_beging) break;
                                    fi_end[i] = fi_beging;
                                    ned = true;
                                    ined = i;
                                }

                                xzdv[i] = xr[i] + r[i] * Math.Cos(fi_beging);
                                if (xzdv[i] > lobj && xr[i] + r[i] * Math.Cos(fi_endg) <= lobj)
                                {
                                    eFi = fi_endg;
                                    bFi = fi_beging;
                                    rFi = r[i]; xFi = xr[i]; yFi = Yr[i]; iFi = i;
                                    inobj = true;
                                }
                                else if (xzdv[i] <= lobj)
                                {
                                    double lri = Math.Abs(r[i] * (fi_endg - fi_beging));
                                    tr += 2 * lri / (Cz[i + 1] + Cz[i]);
                                    lr += lri;
                                }
                                
                                if (ned) break;
                            }
                            else
                            {
                                if (i == Kz.Length - 1)
                                {
                                    fi_beging = 2 * Math.PI - fi_end[i];
                                    r[i] = Cz[i + 1] / (Math.Abs(Kz[i]) * Math.Sin(fi_end[i]));
                                    xr[i] = Cz[i + 1] / (Kz[i] * Math.Tan(fi_end[i])) + 2 * xzdv[i] - xr[i] + r[i] * Math.Cos(Math.PI + fi_end[i]);
                                    if (!inobj) amp *= Kbtm;
                                }
                                else
                                {
                                    fi_beging = 2 * Math.PI - fi_begin[i + 1];
                                    r[i] = Cz[i + 1] / (Math.Abs(Kz[i]) * Math.Sin(fi_begin[i + 1]));
                                    xr[i] = Cz[i + 1] / (Kz[i] * Math.Tan(fi_beging)) + xzdv[i + 1];
                                }
                                fi_endg = 2 * Math.PI - Math.Asin(Math.Abs(Yr[i] + Hz[i]) / r[i]);

                                if (fi_endg == fi_beging) break;

                                xzdv[i] = xr[i] + r[i] * Math.Cos(fi_endg);
                                if (xzdv[i] > lobj && xr[i] + r[i] * Math.Cos(fi_beging) <= lobj)
                                {
                                    eFi = fi_endg;
                                    bFi = fi_beging;
                                    rFi = r[i]; xFi = xr[i]; yFi = Yr[i]; iFi = i;
                                    inobj = true;
                                }
                                else if (xzdv[i] <= lobj)
                                {
                                    double lri = Math.Abs(r[i] * (fi_endg - fi_beging));
                                    tr += 2 * lri / (Cz[i + 1] + Cz[i]);
                                    lr += lri;
                                }

                                fi_begin[i] = Math.Asin(Math.Abs(Yr[i] + Hz[i]) / r[i]);
                            }
                        }
                        nedif = true;
                        #endregion
                    }
                    else
                    {
                        for (int i = 0; i < Kz.Length; i++)
                        {
                            if (Kz[i] < 0)
                            {
                                if (i == 0)
                                {
                                    fi_endg = fi_begin[i];
                                    r[i] = Cz[i] / (Math.Abs(Kz[i]) * Math.Sin(fi_begin[i]));
                                    xr[i] = Cz[i] / (Kz[i] * Math.Tan(fi_endg)) + xzdv[i];
                                    if (!inobj) amp *= Ksrf;
                                }
                                else
                                {
                                    fi_endg = fi_end[i - 1];
                                    r[i] = Cz[i] / (Math.Abs(Kz[i]) * Math.Sin(fi_end[i - 1]));
                                    xr[i] = Cz[i] / (Kz[i] * Math.Tan(fi_endg)) + xzdv[i - 1];
                                }
                                fi_beging = Math.Asin(Math.Abs(Yr[i] + Hz[i + 1]) / r[i]);

                                xzdv[i] = xr[i] + r[i] * Math.Cos(fi_beging);
                                if (xzdv[i] > lobj && xr[i] + r[i] * Math.Cos(fi_endg) <= lobj)
                                {
                                    eFi = fi_endg;
                                    bFi = fi_beging;
                                    rFi = r[i]; xFi = xr[i]; yFi = Yr[i]; iFi = i;
                                    inobj = true;
                                }
                                else if (xzdv[i] <= lobj)
                                {
                                    double lri = Math.Abs(r[i] * (fi_endg - fi_beging));
                                    tr += 2 * lri / (Cz[i + 1] + Cz[i]);
                                    lr += lri;
                                }

                                fi_end[i] = fi_beging;
                            }
                            else
                            {
                                if (i == 0)
                                {
                                    fi_beging = Math.PI + fi_begin[i];
                                    r[i] = Cz[i] / (Math.Abs(Kz[i]) * Math.Sin(fi_begin[i]));
                                    xr[i] = Cz[i] / (Kz[i] * Math.Tan(fi_beging)) + xzdv[i];
                                    if (!inobj) amp *= Ksrf;
                                }
                                else
                                {
                                    fi_beging = Math.PI + fi_end[i - 1];
                                    r[i] = Cz[i] / (Math.Abs(Kz[i]) * Math.Sin(fi_end[i - 1]));
                                    xr[i] = Cz[i] / (Kz[i] * Math.Tan(fi_beging)) + xzdv[i - 1];
                                }
                                if (Math.Abs(Yr[i] + Hz[i + 1]) <= Math.Abs(r[i]))
                                {
                                    fi_end[i] = Math.Asin(Math.Abs(Yr[i] + Hz[i + 1]) / r[i]);
                                    fi_endg = Math.PI + fi_end[i];
                                }
                                else
                                {
                                    fi_begin[i] = Math.Asin(Math.Abs(Yr[i] + Hz[i]) / r[i]);
                                    fi_endg = 2 * Math.PI - fi_begin[i];
                                    ned = true;
                                    ined = i;
                                }

                                xzdv[i] = xr[i] + r[i] * Math.Cos(fi_endg);
                                if (xzdv[i] > lobj && xr[i] + r[i] * Math.Cos(fi_beging) <= lobj)
                                {
                                    eFi = fi_endg;
                                    bFi = fi_beging;
                                    rFi = r[i]; xFi = xr[i]; yFi = Yr[i]; iFi = i;
                                    inobj = true;
                                }
                                else if (xzdv[i] <= lobj)
                                {
                                    double lri = Math.Abs(r[i] * (fi_endg - fi_beging));
                                    tr += 2 * lri / (Cz[i + 1] + Cz[i]);
                                    lr += lri;
                                }
                                
                                if (ned) break;
                            }
                        }

                        if (ned)
                        {
                            #region
                            for (int i = ined - 1; i >= 0; i--)
                            {
                                if (Kz[i] >= 0)
                                {
                                    fi_beging = 2 * Math.PI - fi_begin[i + 1];
                                    r[i] = Cz[i + 1] / (Math.Abs(Kz[i]) * Math.Sin(fi_begin[i + 1]));
                                    xr[i] = Cz[i + 1] / (Kz[i] * Math.Tan(fi_beging)) + xzdv[i + 1];
                                    fi_endg = 2 * Math.PI - Math.Asin(Math.Abs(Yr[i] + Hz[i]) / r[i]);

                                    xzdv[i] = xr[i] + r[i] * Math.Cos(fi_endg);
                                    if (xzdv[i] > lobj && xr[i] + r[i] * Math.Cos(fi_beging) <= lobj)
                                    {
                                        eFi = fi_endg;
                                        bFi = fi_beging;
                                        rFi = r[i]; xFi = xr[i]; yFi = Yr[i]; iFi = i;
                                        inobj = true;
                                    }
                                    else if (xzdv[i] <= lobj)
                                    {
                                        double lri = Math.Abs(r[i] * (fi_endg - fi_beging));
                                        tr += 2 * lri / (Cz[i + 1] + Cz[i]);
                                        lr += lri;
                                    }

                                    fi_begin[i] = Math.Asin(Math.Abs(Yr[i] + Hz[i]) / r[i]);
                                }
                                else
                                {
                                    ned = false;
                                    fi_endg = Math.PI - fi_begin[i + 1];
                                    r[i] = Cz[i + 1] / (Math.Abs(Kz[i]) * Math.Sin(fi_begin[i + 1]));
                                    xr[i] = Cz[i + 1] / (Kz[i] * Math.Tan(fi_endg)) + xzdv[i + 1];

                                    if (Math.Abs(Yr[i] + Hz[i]) < Math.Abs(r[i]))
                                    {
                                        fi_begin[i] = Math.Asin(Math.Abs(Yr[i] + Hz[i]) / r[i]);
                                        fi_beging = Math.PI - fi_begin[i];
                                    }
                                    else
                                    {
                                        fi_beging = Math.Asin(Math.Abs(Yr[i] + Hz[i + 1]) / r[i]);
                                        fi_end[i] = fi_beging;
                                        ned = true;
                                        ined = i;
                                    }

                                    xzdv[i] = xr[i] + r[i] * Math.Cos(fi_beging);
                                    if (xzdv[i] > lobj && xr[i] + r[i] * Math.Cos(fi_endg) <= lobj)
                                    {
                                        eFi = fi_endg;
                                        bFi = fi_beging;
                                        rFi = r[i]; xFi = xr[i]; yFi = Yr[i]; iFi = i;
                                        inobj = true;
                                    }
                                    else if (xzdv[i] <= lobj)
                                    {
                                        double lri = Math.Abs(r[i] * (fi_endg - fi_beging));
                                        tr += 2 * lri / (Cz[i + 1] + Cz[i]);
                                        lr += lri;
                                    }
                                    
                                    if (ned) break;
                                }
                            }
                            #endregion
                        }
                        else
                        {
                            for (int i = Kz.Length - 1; i >= 0; i--)
                            {
                                if (Kz[i] < 0)
                                {
                                    if (i == Kz.Length - 1)
                                    {
                                        fi_endg = Math.PI - fi_end[i];
                                        r[i] = Cz[i + 1] / (Math.Abs(Kz[i]) * Math.Sin(fi_end[i]));
                                        xr[i] = Cz[i + 1] / (Kz[i] * Math.Tan(fi_endg)) + xzdv[i];
                                        if (!inobj) amp *= Kbtm;
                                    }
                                    else
                                    {
                                        fi_endg = Math.PI - fi_begin[i + 1];
                                        r[i] = Cz[i + 1] / (Math.Abs(Kz[i]) * Math.Sin(fi_begin[i + 1]));
                                        xr[i] = Cz[i + 1] / (Kz[i] * Math.Tan(fi_endg)) + xzdv[i + 1];
                                    }

                                    if (Math.Abs(Yr[i] + Hz[i]) < Math.Abs(r[i]))
                                    {
                                        fi_begin[i] = Math.Asin(Math.Abs(Yr[i] + Hz[i]) / r[i]);
                                        fi_beging = Math.PI - fi_begin[i];
                                    }
                                    else
                                    {
                                        fi_beging = Math.Asin(Math.Abs(Yr[i] + Hz[i + 1]) / r[i]);
                                        fi_end[i] = fi_beging;
                                        ned = true;
                                        ined = i;
                                    }

                                    xzdv[i] = xr[i] + r[i] * Math.Cos(fi_beging);
                                    if (xzdv[i] > lobj && xr[i] + r[i] * Math.Cos(fi_endg) <= lobj)
                                    {
                                        eFi = fi_endg;
                                        bFi = fi_beging;
                                        rFi = r[i]; xFi = xr[i]; yFi = Yr[i]; iFi = i;
                                        inobj = true;
                                    }
                                    else if (xzdv[i] <= lobj)
                                    {
                                        double lri = Math.Abs(r[i] * (fi_endg - fi_beging));
                                        tr += 2 * lri / (Cz[i + 1] + Cz[i]);
                                        lr += lri;
                                    }
                                    
                                    if (ned) break;
                                }
                                else
                                {
                                    if (i == Kz.Length - 1)
                                    {
                                        fi_beging = 2 * Math.PI - fi_end[i];
                                        r[i] = Cz[i + 1] / (Math.Abs(Kz[i]) * Math.Sin(fi_end[i]));
                                        xr[i] = Cz[i + 1] / (Kz[i] * Math.Tan(fi_end[i])) + 2 * xzdv[i] - xr[i] + r[i] * Math.Cos(Math.PI + fi_end[i]);
                                        if (!inobj) amp *= Kbtm;
                                    }
                                    else
                                    {
                                        fi_beging = 2 * Math.PI - fi_begin[i + 1];
                                        r[i] = Cz[i + 1] / (Math.Abs(Kz[i]) * Math.Sin(fi_begin[i + 1]));
                                        xr[i] = Cz[i + 1] / (Kz[i] * Math.Tan(fi_beging)) + xzdv[i + 1];
                                    }
                                    fi_endg = 2 * Math.PI - Math.Asin(Math.Abs(Yr[i] + Hz[i]) / r[i]);

                                    xzdv[i] = xr[i] + r[i] * Math.Cos(fi_endg);
                                    if (xzdv[i] > lobj && xr[i] + r[i] * Math.Cos(fi_beging) <= lobj)
                                    {
                                        eFi = fi_endg;
                                        bFi = fi_beging;
                                        rFi = r[i]; xFi = xr[i]; yFi = Yr[i]; iFi = i;
                                        inobj = true;
                                    }
                                    else if (xzdv[i] <= lobj)
                                    {
                                        double lri = Math.Abs(r[i] * (fi_endg - fi_beging));
                                        tr += 2 * lri / (Cz[i + 1] + Cz[i]);
                                        lr += lri;
                                    }

                                    fi_begin[i] = Math.Asin(Math.Abs(Yr[i] + Hz[i]) / r[i]);
                                }
                            }
                        }
                    }
                }
            }

            #endregion

            #region Цикл 2

            nedif = false;
            for (int ii = 0; ii < n; ii++)
            {
                if (Tetgr <= 90 && (bkm90b || Kz[ig] < 0) || Tetgr > 90 && !(mkb90b || Kz[ig] >= 0))
                {
                    bool ned = false;
                    int ined = 0;
                    int zi = 0;
                    for (int i = ig + 1; i < Kz.Length; i++)
                    {
                        if (ii > 0 && !nedif)
                            break;
                        if (Kz[i] < 0)
                        {
                            fi_endg = fi_end[i - 1];
                            r[i] = Cz[i] / (Math.Abs(Kz[i]) * Math.Sin(fi_end[i - 1]));
                            xr[i] = Cz[i] / (Kz[i] * Math.Tan(fi_endg)) + xzdv[i - 1];
                            fi_beging = Math.Asin(Math.Abs(Yr[i] + Hz[i + 1]) / r[i]);
                            if (xr[i] + r[i] * Math.Cos(fi_beging) > lobj && xr[i] + r[i] * Math.Cos(fi_endg) <= lobj)
                            {
                                eFi = fi_endg;
                                bFi = fi_beging;
                                rFi = r[i]; xFi = xr[i]; yFi = Yr[i]; iFi = i;
                                inobj = true;
                            }
                            else if (xr[i] + r[i] * Math.Cos(fi_beging) <= lobj)
                            {
                                double lri = Math.Abs(r[i] * (fi_endg - fi_beging));
                                tr += 2 * lri / (Cz[i + 1] + Cz[i]);
                                lr += lri;
                            }
                            fi_end[i] = fi_beging;
                            xzdv[i] = xr[i] + r[i] * Math.Cos(fi_beging);
                        }
                        else
                        {
                            fi_beging = Math.PI + fi_end[i - 1];
                            r[i] = Cz[i] / (Math.Abs(Kz[i]) * Math.Sin(fi_end[i - 1]));
                            xr[i] = Cz[i] / (Kz[i] * Math.Tan(fi_beging)) + xzdv[i - 1];
                            if (Math.Abs(Yr[i] + Hz[i + 1]) <= Math.Abs(r[i]))
                            {
                                fi_endg = Math.PI + Math.Asin(Math.Abs(Yr[i] + Hz[i + 1]) / r[i]);
                                fi_end[i] = Math.Asin(Math.Abs(Yr[i] + Hz[i + 1]) / r[i]);
                            }
                            else
                            {
                                fi_endg = 2 * Math.PI - Math.Asin(Math.Abs(Yr[i] + Hz[i]) / r[i]);
                                fi_begin[i] = Math.Asin(Math.Abs(Yr[i] + Hz[i]) / r[i]);
                                ned = true;
                                ined = i;
                            }
                            if (xr[i] + r[i] * Math.Cos(fi_endg) > lobj && xr[i] + r[i] * Math.Cos(fi_beging) <= lobj)
                            {
                                eFi = fi_endg;
                                bFi = fi_beging;
                                rFi = r[i]; xFi = xr[i]; yFi = Yr[i]; iFi = i;
                                inobj = true;
                            }
                            else if (xr[i] + r[i] * Math.Cos(fi_endg) <= lobj)
                            {
                                double lri = Math.Abs(r[i] * (fi_endg - fi_beging));
                                tr += 2 * lri / (Cz[i + 1] + Cz[i]);
                                lr += lri;
                            }
                            xzdv[i] = xr[i] + r[i] * Math.Cos(fi_endg);
                            if (ned) break;
                        }
                        zi = i;
                    }

                    nedif = false;
                    if (ned)
                    {
                        #region
                        for (int i = ined - 1; i >= 0; i--)
                        {
                            if (Kz[i] >= 0)
                            {
                                fi_beging = 2 * Math.PI - fi_begin[i + 1];
                                r[i] = Cz[i + 1] / (Math.Abs(Kz[i]) * Math.Sin(fi_begin[i + 1]));
                                xr[i] = Cz[i + 1] / (Kz[i] * Math.Tan(fi_beging)) + xzdv[i + 1];
                                fi_endg = 2 * Math.PI - Math.Asin(Math.Abs(Yr[i] + Hz[i]) / r[i]);

                                xzdv[i] = xr[i] + r[i] * Math.Cos(fi_endg);
                                if (xzdv[i] > lobj && xr[i] + r[i] * Math.Cos(fi_beging) <= lobj)
                                {
                                    eFi = fi_endg;
                                    bFi = fi_beging;
                                    rFi = r[i]; xFi = xr[i]; yFi = Yr[i]; iFi = i;
                                    inobj = true;
                                }
                                else if (xzdv[i] <= lobj)
                                {
                                    double lri = Math.Abs(r[i] * (fi_endg - fi_beging));
                                    tr += 2 * lri / (Cz[i + 1] + Cz[i]);
                                    lr += lri;
                                }

                                fi_begin[i] = Math.Asin(Math.Abs(Yr[i] + Hz[i]) / r[i]);
                            }
                            else
                            {
                                ned = false;
                                fi_endg = Math.PI - fi_begin[i + 1];
                                r[i] = Cz[i + 1] / (Math.Abs(Kz[i]) * Math.Sin(fi_begin[i + 1]));
                                xr[i] = Cz[i + 1] / (Kz[i] * Math.Tan(fi_endg)) + xzdv[i + 1];

                                if (Math.Abs(Yr[i] + Hz[i]) < Math.Abs(r[i]))
                                {
                                    fi_begin[i] = Math.Asin(Math.Abs(Yr[i] + Hz[i]) / r[i]);
                                    fi_beging = Math.PI - fi_begin[i];
                                }
                                else
                                {
                                    fi_beging = Math.Asin(Math.Abs(Yr[i] + Hz[i + 1]) / r[i]);
                                    fi_end[i] = fi_beging;
                                    ned = true;
                                    ined = i;
                                }

                                xzdv[i] = xr[i] + r[i] * Math.Cos(fi_beging);
                                if (xzdv[i] > lobj && xr[i] + r[i] * Math.Cos(fi_endg) <= lobj)
                                {
                                    eFi = fi_endg;
                                    bFi = fi_beging;
                                    rFi = r[i]; xFi = xr[i]; yFi = Yr[i]; iFi = i;
                                    inobj = true;
                                }
                                else if (xzdv[i] <= lobj)
                                {
                                    double lri = Math.Abs(r[i] * (fi_endg - fi_beging));
                                    tr += 2 * lri / (Cz[i + 1] + Cz[i]);
                                    lr += lri;
                                }
                                
                                if (ned)
                                {
                                    zi = i;
                                    break;
                                }
                            }
                        }

                        if (ned)     // якщо промінь заломився та змінив напрям згори вниз
                        {
                            for (int i = ined + 1; i <= ig; i++)    // цикл проходить вниз по водних шарах
                            {
                                if (Kz[i] < 0)                // коефіцієнт зміни швидкості звуку за глибиною від'ємний
                                {                            // промінь вигинається вгору
                                    fi_endg = fi_end[i - 1];
                                    r[i] = Cz[i] / (Math.Abs(Kz[i]) * Math.Sin(fi_end[i - 1]));
                                    xr[i] = Cz[i] / (Kz[i] * Math.Tan(fi_endg)) + xzdv[i - 1];
                                    fi_beging = Math.Asin(Math.Abs(Yr[i] + Hz[i + 1]) / r[i]);

                                    xzdv[i] = xr[i] + r[i] * Math.Cos(fi_beging);
                                    if (xzdv[i] > lobj && xr[i] + r[i] * Math.Cos(fi_endg) <= lobj)
                                    {
                                        eFi = fi_endg;
                                        bFi = fi_beging;
                                        rFi = r[i]; xFi = xr[i]; yFi = Yr[i]; iFi = i;
                                        inobj = true;
                                    }
                                    else if (xzdv[i] <= lobj)
                                    {
                                        double lri = Math.Abs(r[i] * (fi_endg - fi_beging));
                                        tr += 2 * lri / (Cz[i + 1] + Cz[i]);
                                        lr += lri;
                                    }

                                    fi_end[i] = fi_beging;
                                }
                                else                         // коефіцієнт зміни швидкості звуку не менше нуля, промінь вигинається вниз
                                {                            // в даному випадку можлива ситуація, коли промінь знову заломиться у водному шарі
                                    ned = false;             // та змінить напрям знизу вгору
                                    fi_beging = Math.PI + fi_end[i - 1];
                                    r[i] = Cz[i] / (Math.Abs(Kz[i]) * Math.Sin(fi_end[i - 1]));
                                    xr[i] = Cz[i] / (Kz[i] * Math.Tan(fi_beging)) + xzdv[i - 1];

                                    if (Math.Abs(Yr[i] + Hz[i + 1]) <= Math.Abs(r[i]))   // якщо радіус кола не менший за нижню межу водного шару
                                    {   // промінь пройшов крізь водний шар
                                        fi_end[i] = Math.Asin(Math.Abs(Yr[i] + Hz[i + 1]) / r[i]);
                                        fi_endg = Math.PI + fi_end[i];
                                    }
                                    else    // якщо радіус кола менший за нижню межу водного шару, то промінь не проходить крізь водний шар
                                    {       // а заломлюється в ньому та змінює напрям знизу вгору
                                        fi_begin[i] = Math.Asin(Math.Abs(Yr[i] + Hz[i]) / r[i]);
                                        fi_endg = 2 * Math.PI - fi_begin[i];
                                        ned = true;          // промінь змінив напрям на протилежний
                                        ined = i;            // зберігаємо номер водного шару, в якому промінь змінив напрям знизу вгору
                                    }

                                    xzdv[i] = xr[i] + r[i] * Math.Cos(fi_endg);
                                    if (xzdv[i] > lobj && xr[i] + r[i] * Math.Cos(fi_beging) <= lobj)
                                    {
                                        eFi = fi_endg;
                                        bFi = fi_beging;
                                        rFi = r[i]; xFi = xr[i]; yFi = Yr[i]; iFi = i;
                                        inobj = true;
                                    }
                                    else if (xzdv[i] <= lobj)
                                    {
                                        double lri = Math.Abs(r[i] * (fi_endg - fi_beging));
                                        tr += 2 * lri / (Cz[i + 1] + Cz[i]);
                                        lr += lri;
                                    }
                                    
                                    if (ned) break;     // якщо промінь заломився та змінив напрям на протилежний, то виходимо з циклу
                                }
                                zi = i;
                            }                            // кінець циклу, який проходить вниз по водних шарах
                        }

                        ned = false;
                        for (int i = 0; i < ined - 1 && zi < ined - 1 && Math.Abs(ined - ig) > 1 || i < ined && zi < ined && !(Math.Abs(ined - ig) > 1); i++)
                        {
                            if (Kz[i] < 0)
                            {
                                if (i == 0)
                                {
                                    fi_endg = fi_begin[i];
                                    r[i] = Cz[i] / (Math.Abs(Kz[i]) * Math.Sin(fi_begin[i]));
                                    xr[i] = Cz[i] / (Kz[i] * Math.Tan(fi_endg)) + xzdv[i];
                                    if (!inobj) amp *= Ksrf;
                                }
                                else
                                {
                                    fi_endg = fi_end[i - 1];
                                    r[i] = Cz[i] / (Math.Abs(Kz[i]) * Math.Sin(fi_end[i - 1]));
                                    xr[i] = Cz[i] / (Kz[i] * Math.Tan(fi_endg)) + xzdv[i - 1];
                                }
                                fi_beging = Math.Asin(Math.Abs(Yr[i] + Hz[i + 1]) / r[i]);

                                xzdv[i] = xr[i] + r[i] * Math.Cos(fi_beging);
                                if (xzdv[i] > lobj && xr[i] + r[i] * Math.Cos(fi_endg) <= lobj)
                                {
                                    eFi = fi_endg;
                                    bFi = fi_beging;
                                    rFi = r[i]; xFi = xr[i]; yFi = Yr[i]; iFi = i;
                                    inobj = true;
                                }
                                else if (xzdv[i] <= lobj)
                                {
                                    double lri = Math.Abs(r[i] * (fi_endg - fi_beging));
                                    tr += 2 * lri / (Cz[i + 1] + Cz[i]);
                                    lr += lri;
                                }

                                fi_end[i] = fi_beging;
                            }
                            else
                            {
                                if (i == 0)
                                {
                                    fi_beging = Math.PI + fi_begin[i];
                                    r[i] = Cz[i] / (Math.Abs(Kz[i]) * Math.Sin(fi_begin[i]));
                                    xr[i] = Cz[i] / (Kz[i] * Math.Tan(fi_beging)) + xzdv[i];
                                    if (!inobj) amp *= Ksrf;
                                }
                                else
                                {
                                    fi_beging = Math.PI + fi_end[i - 1];
                                    r[i] = Cz[i] / (Math.Abs(Kz[i]) * Math.Sin(fi_end[i - 1]));
                                    xr[i] = Cz[i] / (Kz[i] * Math.Tan(fi_beging)) + xzdv[i - 1];
                                }
                                if (Math.Abs(Yr[i] + Hz[i + 1]) <= Math.Abs(r[i]))
                                {
                                    fi_endg = Math.PI + Math.Asin(Math.Abs(Yr[i] + Hz[i + 1]) / r[i]);
                                    if (fi_endg == fi_beging) break;
                                    fi_end[i] = Math.Asin(Math.Abs(Yr[i] + Hz[i + 1]) / r[i]);
                                }
                                else
                                {
                                    fi_endg = 2 * Math.PI - Math.Asin(Math.Abs(Yr[i] + Hz[i]) / r[i]);
                                    if (fi_endg == fi_beging) break;
                                    fi_begin[i] = Math.Asin(Math.Abs(Yr[i] + Hz[i]) / r[i]);
                                    ned = true;
                                    ined = i;
                                }

                                xzdv[i] = xr[i] + r[i] * Math.Cos(fi_endg);
                                if (xzdv[i] > lobj && xr[i] + r[i] * Math.Cos(fi_beging) <= lobj)
                                {
                                    eFi = fi_endg;
                                    bFi = fi_beging;
                                    rFi = r[i]; xFi = xr[i]; yFi = Yr[i]; iFi = i;
                                    inobj = true;
                                }
                                else if (xzdv[i] <= lobj)
                                {
                                    double lri = Math.Abs(r[i] * (fi_endg - fi_beging));
                                    tr += 2 * lri / (Cz[i + 1] + Cz[i]);
                                    lr += lri;
                                }
                                
                                if (ned) break;
                            }
                        }
                        nedif = true;
                        #endregion
                    }
                    else
                    {
                        for (int i = Kz.Length - 1; i >= 0; i--)
                        {
                            if (Kz[i] < 0)
                            {
                                if (i == Kz.Length - 1)
                                {
                                    fi_endg = Math.PI - fi_end[i];
                                    r[i] = Cz[i + 1] / (Math.Abs(Kz[i]) * Math.Sin(fi_end[i]));
                                    xr[i] = Cz[i + 1] / (Kz[i] * Math.Tan(fi_endg)) + xzdv[i];
                                    if (!inobj) amp *= Kbtm;
                                }
                                else
                                {
                                    fi_endg = Math.PI - fi_begin[i + 1];
                                    r[i] = Cz[i + 1] / (Math.Abs(Kz[i]) * Math.Sin(fi_begin[i + 1]));
                                    xr[i] = Cz[i + 1] / (Kz[i] * Math.Tan(fi_endg)) + xzdv[i + 1];
                                }

                                if (Math.Abs(Yr[i] + Hz[i]) < Math.Abs(r[i]))
                                {
                                    fi_begin[i] = Math.Asin(Math.Abs(Yr[i] + Hz[i]) / r[i]);
                                    fi_beging = Math.PI - fi_begin[i];
                                }
                                else
                                {
                                    fi_beging = Math.Asin(Math.Abs(Yr[i] + Hz[i + 1]) / r[i]);
                                    fi_end[i] = fi_beging;
                                    ned = true;
                                    ined = i;
                                }

                                xzdv[i] = xr[i] + r[i] * Math.Cos(fi_beging);
                                if (xzdv[i] > lobj && xr[i] + r[i] * Math.Cos(fi_endg) <= lobj)
                                {
                                    eFi = fi_endg;
                                    bFi = fi_beging;
                                    rFi = r[i]; xFi = xr[i]; yFi = Yr[i]; iFi = i;
                                    inobj = true;
                                }
                                else if (xzdv[i] <= lobj)
                                {
                                    double lri = Math.Abs(r[i] * (fi_endg - fi_beging));
                                    tr += 2 * lri / (Cz[i + 1] + Cz[i]);
                                    lr += lri;
                                }
                                
                                if (ned) break;
                            }
                            else
                            {
                                if (i == Kz.Length - 1)
                                {
                                    fi_beging = 2 * Math.PI - fi_end[i];
                                    r[i] = Cz[i + 1] / (Math.Abs(Kz[i]) * Math.Sin(fi_end[i]));
                                    xr[i] = Cz[i + 1] / (Kz[i] * Math.Tan(fi_end[i])) + 2 * xzdv[i] - xr[i] + r[i] * Math.Cos(Math.PI + fi_end[i]);
                                    if (!inobj) amp *= Kbtm;
                                }
                                else
                                {
                                    fi_beging = 2 * Math.PI - fi_begin[i + 1];
                                    r[i] = Cz[i + 1] / (Math.Abs(Kz[i]) * Math.Sin(fi_begin[i + 1]));
                                    xr[i] = Cz[i + 1] / (Kz[i] * Math.Tan(fi_beging)) + xzdv[i + 1];
                                }
                                fi_endg = 2 * Math.PI - Math.Asin(Math.Abs(Yr[i] + Hz[i]) / r[i]);

                                xzdv[i] = xr[i] + r[i] * Math.Cos(fi_endg);
                                if (xzdv[i] > lobj && xr[i] + r[i] * Math.Cos(fi_beging) <= lobj)
                                {
                                    eFi = fi_endg;
                                    bFi = fi_beging;
                                    rFi = r[i]; xFi = xr[i]; yFi = Yr[i]; iFi = i;
                                    inobj = true;
                                }
                                else if (xzdv[i] <= lobj)
                                {
                                    double lri = Math.Abs(r[i] * (fi_endg - fi_beging));
                                    tr += 2 * lri / (Cz[i + 1] + Cz[i]);
                                    lr += lri;
                                }
                                fi_begin[i] = Math.Asin(Math.Abs(Yr[i] + Hz[i]) / r[i]);
                            }
                        }

                        if (ned)
                        {
                            #region
                            for (int i = ined + 1; i < Kz.Length; i++)
                            {
                                if (Kz[i] < 0)
                                {
                                    fi_endg = fi_end[i - 1];
                                    r[i] = Cz[i] / (Math.Abs(Kz[i]) * Math.Sin(fi_end[i - 1]));
                                    xr[i] = Cz[i] / (Kz[i] * Math.Tan(fi_endg)) + xzdv[i - 1];
                                    fi_beging = Math.Asin(Math.Abs(Yr[i] + Hz[i + 1]) / r[i]);

                                    xzdv[i] = xr[i] + r[i] * Math.Cos(fi_beging);
                                    if (xzdv[i] > lobj && xr[i] + r[i] * Math.Cos(fi_endg) <= lobj)
                                    {
                                        eFi = fi_endg;
                                        bFi = fi_beging;
                                        rFi = r[i]; xFi = xr[i]; yFi = Yr[i]; iFi = i;
                                        inobj = true;
                                    }
                                    else if (xzdv[i] <= lobj)
                                    {
                                        double lri = Math.Abs(r[i] * (fi_endg - fi_beging));
                                        tr += 2 * lri / (Cz[i + 1] + Cz[i]);
                                        lr += lri;
                                    }

                                    fi_end[i] = fi_beging;
                                }
                                else
                                {
                                    ned = false;
                                    fi_beging = Math.PI + fi_end[i - 1];
                                    r[i] = Cz[i] / (Math.Abs(Kz[i]) * Math.Sin(fi_end[i - 1]));
                                    xr[i] = Cz[i] / (Kz[i] * Math.Tan(fi_beging)) + xzdv[i - 1];

                                    if (Math.Abs(Yr[i] + Hz[i + 1]) <= Math.Abs(r[i]))
                                    {
                                        fi_end[i] = Math.Asin(Math.Abs(Yr[i] + Hz[i + 1]) / r[i]);
                                        fi_endg = Math.PI + fi_end[i];
                                    }
                                    else
                                    {
                                        fi_begin[i] = Math.Asin(Math.Abs(Yr[i] + Hz[i]) / r[i]);
                                        fi_endg = 2 * Math.PI - fi_begin[i];
                                        ned = true;
                                        ined = i;
                                    }

                                    xzdv[i] = xr[i] + r[i] * Math.Cos(fi_endg);
                                    if (xzdv[i] > lobj && xr[i] + r[i] * Math.Cos(fi_beging) <= lobj)
                                    {
                                        eFi = fi_endg;
                                        bFi = fi_beging;
                                        rFi = r[i]; xFi = xr[i]; yFi = Yr[i]; iFi = i;
                                        inobj = true;
                                    }
                                    else if (xzdv[i] <= lobj)
                                    {
                                        double lri = Math.Abs(r[i] * (fi_endg - fi_beging));
                                        tr += 2 * lri / (Cz[i + 1] + Cz[i]);
                                        lr += lri;
                                    }
                                    
                                    if (ned) break;
                                }
                            }
                            #endregion
                        }
                        else
                        {
                            for (int i = 0; i < Kz.Length; i++)
                            {
                                if (Kz[i] < 0)
                                {
                                    if (i == 0)
                                    {
                                        fi_endg = fi_begin[i];
                                        r[i] = Cz[i] / (Math.Abs(Kz[i]) * Math.Sin(fi_begin[i]));
                                        xr[i] = Cz[i] / (Kz[i] * Math.Tan(fi_endg)) + xzdv[i];
                                        if (!inobj) amp *= Ksrf;
                                    }
                                    else
                                    {
                                        fi_endg = fi_end[i - 1];
                                        r[i] = Cz[i] / (Math.Abs(Kz[i]) * Math.Sin(fi_end[i - 1]));
                                        xr[i] = Cz[i] / (Kz[i] * Math.Tan(fi_endg)) + xzdv[i - 1];
                                    }
                                    fi_beging = Math.Asin(Math.Abs(Yr[i] + Hz[i + 1]) / r[i]);

                                    xzdv[i] = xr[i] + r[i] * Math.Cos(fi_beging);
                                    if (xzdv[i] > lobj && xr[i] + r[i] * Math.Cos(fi_endg) <= lobj)
                                    {
                                        eFi = fi_endg;
                                        bFi = fi_beging;
                                        rFi = r[i]; xFi = xr[i]; yFi = Yr[i]; iFi = i;
                                        inobj = true;
                                    }
                                    else if (xzdv[i] <= lobj)
                                    {
                                        double lri = Math.Abs(r[i] * (fi_endg - fi_beging));
                                        tr += 2 * lri / (Cz[i + 1] + Cz[i]);
                                        lr += lri;
                                    }

                                    fi_end[i] = fi_beging;
                                }
                                else
                                {
                                    if (i == 0)
                                    {
                                        fi_beging = Math.PI + fi_begin[i];
                                        r[i] = Cz[i] / (Math.Abs(Kz[i]) * Math.Sin(fi_begin[i]));
                                        xr[i] = Cz[i] / (Kz[i] * Math.Tan(fi_beging)) + xzdv[i];
                                        if (!inobj) amp *= Ksrf;
                                    }
                                    else
                                    {
                                        fi_beging = Math.PI + fi_end[i - 1];
                                        r[i] = Cz[i] / (Math.Abs(Kz[i]) * Math.Sin(fi_end[i - 1]));
                                        xr[i] = Cz[i] / (Kz[i] * Math.Tan(fi_beging)) + xzdv[i - 1];
                                    }

                                    if (Math.Abs(Yr[i] + Hz[i + 1]) <= Math.Abs(r[i]))
                                    {
                                        fi_end[i] = Math.Asin(Math.Abs(Yr[i] + Hz[i + 1]) / r[i]);
                                        fi_endg = Math.PI + fi_end[i];
                                    }
                                    else
                                    {
                                        fi_begin[i] = Math.Asin(Math.Abs(Yr[i] + Hz[i]) / r[i]);
                                        fi_endg = 2 * Math.PI - fi_begin[i];
                                        ned = true;
                                        ined = i;
                                    }

                                    xzdv[i] = xr[i] + r[i] * Math.Cos(fi_endg);
                                    if (xzdv[i] > lobj && xr[i] + r[i] * Math.Cos(fi_beging) <= lobj)
                                    {
                                        eFi = fi_endg;
                                        bFi = fi_beging;
                                        rFi = r[i]; xFi = xr[i]; yFi = Yr[i]; iFi = i;
                                        inobj = true;
                                    }
                                    else if (xzdv[i] <= lobj)
                                    {
                                        double lri = Math.Abs(r[i] * (fi_endg - fi_beging));
                                        tr += 2 * lri / (Cz[i + 1] + Cz[i]);
                                        lr += lri;
                                    }
                                    
                                    if (ned) break;
                                }
                            }
                        }
                    }
                }
            }

            #endregion
        }



        public bool BuildRayGas(double Tetg, double lob, double hob, out double tmray, out double Lr, out double amp, out double ttgrd)
        {
            int nv = 1;
            double tmr;
            int zkl = 1 + (int)(Tetg * 100 + 10);
            ttgrd = Tetg;
            double tgrd = 90 + Tetg + 0.1;
            int step = 1;
            bool ingas = false; 
            do
            {
                double bFi, eFi, rFi, xFi, yFi;
                int iFi;

                amp = 1;
                BuildRay(lob, tgrd, nv, out Lr, out tmr, out amp, out bFi, out eFi, out rFi, out xFi, out yFi, out iFi);
                if (amp < 0.1)
                    break;
                if (xFi + rFi * Math.Cos(bFi) < lob && xFi + rFi * Math.Cos(eFi) < lob)
                {
                    nv++;
                    continue;
                }
                double dfi = (eFi - bFi) / 360.0;
                for (double fi = bFi; fi <= eFi; fi += dfi)
                {
                    //double fa = Math.Atan2(hob - yFi, lob - xFi);
                    //double fi = fa + 2 * Math.PI;
                    if (Math.Abs(xFi + rFi * Math.Cos(fi) - lob) < 0.1 && Math.Abs(Math.Abs(yFi + rFi * Math.Sin(fi)) - hob) < 0.1)
                    {

                        // if(fai>0.01)
                        // listf.Add(fai);
                        ingas = true;
                        ttgrd = tgrd;
                        double lri = (Math.Abs(rFi * Math.Cos(eFi)) > Math.Abs(rFi * Math.Cos(bFi))) ? Math.Abs(rFi * (fi - bFi)) : Math.Abs(rFi * (fi - eFi));
                        Lr += lri;
                        tmr += 2 * lri / (Cz[iFi + 1] + Cz[iFi]);
                    }
                }
                if (!ingas)
                {
                    tgrd = 90 + Math.Pow(-1, step) * 0.01 * zkl;
                    if (step % 2 == 0)
                        zkl++;
                    step++;
                }
            } while (!ingas);
            tmray = tmr;
            return ingas;
        }
    }



    public class GObject
    {
        public void calcAmp(    double hobj, double lobj, double timj,
                                List<double> AMP,
                                List<double> TIM,
                                List<double> ANG,
                                List<double> LNG)
        {
            Ray r1 = new Ray();
            // List<double> AMP = new List<double>();
            // List<double> listTime = new List<double>();
            // List<double> listAngl = new List<double>();
            // List<double> listLen = new List<double>();
            /*double dummy = Ray.Ksrf * Ray.Kbtm;
            int nvr = 0;
            while (dummy > 0.1)
            {
                dummy *= dummy;
                nvr++;
            }*/
            double ttgrd = 0;
            double tmray1 = 0;
            bool ingas = true;
            while(ingas)
            {
                double ttgrd1;
                double amp1 = 1;
                double lr;
                ingas = r1.BuildRayGas(ttgrd, lobj, hobj, out tmray1, out lr, out amp1, out ttgrd1);
                if (ingas)
                {
                    AMP.Add(amp1);
                    TIM.Add(tmray1);
                    LNG.Add(lr);

                    // TODO:
                    if (ttgrd1 < 90)
                        ANG.Add((270 - ttgrd1) * Math.PI / 180);
                    else
                        ANG.Add((90-ttgrd1) * Math.PI / 180);
                }
                else
                    break;

                ttgrd = Math.Abs(90 - ttgrd1);
            }
            // ampry = new double[listAmp.Count];
            // tmry = new double[listTime.Count];
            // lengthry = new double[listLen.Count];
            // anglry = new double[listAngl.Count];

            // listAmp.CopyTo(ampry);
            // listTime.CopyTo(tmry);
            // listLen.CopyTo(lengthry);
            // listAngl.CopyTo(anglry);
        }
    }
}