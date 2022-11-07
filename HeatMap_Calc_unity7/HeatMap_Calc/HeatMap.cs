using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using System.Threading;
using System.Drawing;

namespace HeatMap_Calc
{
    public partial class Calc_Class
    {
        public Bitmap HeatMap_BitMap;//本体に送るbitmapデータ
        float[,] pointvalue;//各座標の重みつけ計算値
        float[,] pointvalue_calc;//各座標の重みつけ計算途中用
        float[] PointvalueColor;//RGB格納配列
        public int ProgressValue_HeatMap = 0;
        int ResolutionX;
        int ResolutionY;
        //int[] ColorPoint;//値がある座標

        //LookupTable作成
        unsafe public int[,] LookupTable(int[,] LUT,int radius)
        {
            int i;
            int heatmap_weight = 0;
            int x = radius;
            int y = 0;
            int decisionOver2 = 1 - x;   // Decision criterion divided by 2 evaluated at x=r, y=0
            double d = 0;//中心からの距離
            double d_x1;
            double d_x2;
            double d_y1;
            double d_y2;
            //LUT = new int[radius * 2 + 1, radius * 2 + 1];

            i = 0;

            fixed (int* pLUT = &LUT[0, 0])
            {
                while (y <= x)
                {
                    d_x1 = Math.Pow(x, 2);
                    d_x2 = Math.Pow(-x, 2);
                    d_y1 = Math.Pow(y, 2);
                    d_y2 = Math.Pow(-y, 2);
                    ///////////////////////////////中心座標はx+r,y+r                
                    for (i = -y; i < y; i++)
                    {
                        d = Math.Sqrt(Math.Pow(i, 2) + d_x1);
                        if (radius - (int)d + heatmap_weight > 0)
                        {
                            pLUT[(i + radius) * (radius * 2 + 1) + x + radius] = ((radius - (int)d) + heatmap_weight);
                        }
                        else
                        {
                            pLUT[(i + radius) * (radius * 2 + 1) + x + radius] = 1;
                        }

                        d = Math.Sqrt(Math.Pow(i, 2) + d_x2);
                        if (radius - (int)d + heatmap_weight > 0)
                        {
                            pLUT[(i + radius) * (radius * 2 + 1) - x + radius] = (int)((radius - (int)d) + heatmap_weight);
                        }
                        else
                        {
                            pLUT[(i + radius) * (radius * 2 + 1) - x + radius] = 1;
                        }
                    }

                    for (i = -x; i < x; i++)
                    {
                        d = Math.Sqrt(Math.Pow(i, 2) + d_y1);
                        if (radius - (int)d + heatmap_weight > 0)
                        {
                            pLUT[(i + radius) * (radius * 2 + 1) + y + radius] = (int)((radius - (int)d) + heatmap_weight);
                        }
                        else
                        {
                            pLUT[(i + radius) * (radius * 2 + 1) + y + radius] = 1;
                        }
                        d = Math.Sqrt(Math.Pow(i, 2) + d_y2);
                        if (radius - (int)d + heatmap_weight > 0)
                        {
                            pLUT[(i + radius) * (radius * 2 + 1) - y + radius] = (int)((radius - (int)d) + heatmap_weight);
                        }
                        else
                        {
                            pLUT[(i + radius) * (radius * 2 + 1) - y + radius] = 1;
                        }
                    }

                    y++;
                    if (decisionOver2 <= 0)
                    {
                        decisionOver2 += 2 * y + 1;   // Change in decision criterion for y -> y+1
                    }
                    else
                    {
                        x--;
                        decisionOver2 += 2 * (y - x) + 1;   // Change for y -> y+1, x -> x-1
                    }
                }
            }
            return LUT;
        }

        int[,] LUT;
        //        unsafe public void Calc(int[,] CSV_DataXY, int[] Resolution, int DataCount, int radius,bool heatmap_bool, bool gazeplot_bool, string date, string log_location)//(CSVの視線データXY,Time,解像度,データの総数,radius,ヒートマップの有無，ゲイズプロットの有無, 日付, Logフォルダ名)
        unsafe public void Calc(int[,] CSV_DataXY, int[] Resolution, int DataCount, int radius, bool heatmap_bool, bool line_bool, bool gazeplot_bool, string csvName)//(CSVの視線データXY,Time,解像度,データの総数,radius,ヒートマップの有無,line bool，ゲイズプロットの有無, 日付, Logフォルダ名)
        {
            LUT = new int[radius * 2 + 1, radius * 2 + 1];
            LUT = LookupTable(LUT,radius);
            int i, j, k;
            int DataSetX;//ポインタ上の座標計算用
            int DataSetY;//ポインタ上の座標計算用
            pointvalue = new float[Resolution[0], Resolution[1]];
            pointvalue_calc = new float[Resolution[0], Resolution[1]];
            PointvalueColor = new float[Resolution[0] * Resolution[1] * 3];//RGB格納配列
            ResolutionX = Resolution[0];
            ResolutionY = Resolution[1];
            fixed (float* pPointvalue = &pointvalue[0, 0])
            {
                fixed (int* pDataSet = &CSV_DataXY[0, 0])
                {
                    fixed (int* pLookUpTable = &LUT[0, 0])
                    {
                        for (i = 0; i < DataCount; i++)
                        {

                            DataSetX = (int)pDataSet[i * 2] * Resolution[1];
                            DataSetY = (int)pDataSet[i * 2 + 1];

                            switch (pDataSet[i * 2] > 0 && pDataSet[i * 2] < Resolution[0] && pDataSet[i * 2 + 1] > 0 && pDataSet[i * 2 + 1] < Resolution[1])
                            {
                                case true:
                                    {
                                        for (j = -radius; j < radius; j++)
                                        {
                                            for (k = -radius; k < radius; k++)
                                            {
                                                if (DataSetY + k > 0 && DataSetY + k < ResolutionY && (int)pDataSet[i * 2] + j > 0 && (int)pDataSet[i * 2] + j < ResolutionX)
                                                {
                                                    pPointvalue[DataSetX + DataSetY + (j * Resolution[1]) + k] += pLookUpTable[(j + radius) + ((k + radius) * (radius * 2 + 1))];//重みづけ
                                                }
                                            }
                                        }
                                        break;
                                    }
                                default:
                                    break;
                            }
                        }
                    }
                }
            }



            int canvasStride2;
            int canvasStride3;

            //////////////////////////////////////////////////////////////////////////////////////////bitmapに色データを移し替える処理
            HeatMap_BitMap = new Bitmap(Resolution[0], Resolution[1], System.Drawing.Imaging.PixelFormat.Format24bppRgb);//canvasを作る
            Bitmap HeatMap_BitMap2 = new Bitmap(Resolution[0], Resolution[1], System.Drawing.Imaging.PixelFormat.Format32bppRgb);


            //bitmapのメモリをロックする
            System.Drawing.Imaging.BitmapData canvasData = HeatMap_BitMap.LockBits(new Rectangle(0, 0, HeatMap_BitMap.Width, HeatMap_BitMap.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, HeatMap_BitMap.PixelFormat);

            System.Drawing.Imaging.BitmapData canvasData2 = HeatMap_BitMap2.LockBits(new Rectangle(0, 0, HeatMap_BitMap2.Width, HeatMap_BitMap2.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, HeatMap_BitMap2.PixelFormat);

            //ポインタを取得
            byte* canvasScan1 = (byte*)canvasData.Scan0.ToPointer();
            //スキャンラインのバイト数

            canvasStride2 = canvasData.Stride;




           //ポインタを取得
           byte* canvasScan2 = (byte*)canvasData2.Scan0.ToPointer();
           //スキャンラインのバイト数

           canvasStride3 = canvasData2.Stride;

            if (heatmap_bool == true)
            {

                fixed (float* pPointvalue = &pointvalue[0, 0])
                {
                    fixed (float* pPointvalueColor = &PointvalueColor[0])
                    {
                        for (i = 0; i < HeatMap_BitMap.Height; i++)
                        {
                            for (j = 0; j < HeatMap_BitMap.Width; j++)
                            {
                                if (pPointvalue[(j * (Resolution[1])) + i] < 0)
                                {
                                    pPointvalueColor[(j * (Resolution[1] * 3)) + (i * 3 + 0)] = 0;//R
                                    pPointvalueColor[(j * (Resolution[1] * 3)) + (i * 3 + 1)] = 0;//G
                                    pPointvalueColor[(j * (Resolution[1] * 3)) + (i * 3 + 2)] = 255;//B
                                }
                                else if (pPointvalue[(j * (Resolution[1])) + i] > 0 && pPointvalue[(j * (Resolution[1])) + i] < 64)//青
                                {
                                    pPointvalueColor[(j * (Resolution[1] * 3)) + (i * 3 + 0)] = 0;//R
                                    pPointvalueColor[(j * (Resolution[1] * 3)) + (i * 3 + 1)] = ((int)pPointvalue[(j * (Resolution[1])) + i] * 4) - 1;//G
                                    pPointvalueColor[(j * (Resolution[1] * 3)) + (i * 3 + 2)] = 255;//B
                                }
                                else if (pPointvalue[(j * (Resolution[1])) + i] >= 64 && pPointvalue[(j * (Resolution[1])) + i] < 128)//青　緑
                                {
                                    pPointvalueColor[(j * (Resolution[1] * 3)) + (i * 3 + 0)] = 0;//R
                                    pPointvalueColor[(j * (Resolution[1] * 3)) + (i * 3 + 1)] = 255;//G
                                    pPointvalueColor[(j * (Resolution[1] * 3)) + (i * 3 + 2)] = (255 - 1 - ((int)pPointvalue[(j * (Resolution[1])) + i] - 64) * 4);//B
                                }
                                else if (pPointvalue[(j * (Resolution[1])) + i] >= 128 && pPointvalue[(j * (Resolution[1])) + i] < 192)//緑 赤
                                {
                                    pPointvalueColor[(j * (Resolution[1] * 3)) + (i * 3 + 0)] = ((int)(pPointvalue[(j * (Resolution[1])) + i] - 128) * 4);//R
                                    pPointvalueColor[(j * (Resolution[1] * 3)) + (i * 3 + 1)] = 255;//G
                                    pPointvalueColor[(j * (Resolution[1] * 3)) + (i * 3 + 2)] = 0;//B
                                }
                                else if (pPointvalue[(j * (Resolution[1])) + i] >= 192 && pPointvalue[(j * (Resolution[1])) + i] < 255)//赤
                                {
                                    pPointvalueColor[(j * (Resolution[1] * 3)) + (i * 3 + 0)] = 255;//R
                                    pPointvalueColor[(j * (Resolution[1] * 3)) + (i * 3 + 1)] = (255 - 1 - ((int)pPointvalue[(j * (Resolution[1])) + i] - 192) * 4);//G
                                    pPointvalueColor[(j * (Resolution[1] * 3)) + (i * 3 + 2)] = 0;//B
                                }
                                else if (pPointvalue[(j * (Resolution[1])) + i] >= 255)
                                {
                                    pPointvalueColor[(j * (Resolution[1] * 3)) + (i * 3 + 0)] = 255;//R
                                    pPointvalueColor[(j * (Resolution[1] * 3)) + (i * 3 + 1)] = 0;//G
                                    pPointvalueColor[(j * (Resolution[1] * 3)) + (i * 3 + 2)] = 0;//B
                                }
                                canvasScan2[((Resolution[1] - 1 - i) * canvasStride3) + (j * 4 + 3)] = 126;  // a
                                canvasScan2[((Resolution[1] - 1 - i) * canvasStride3) + (j * 4 + 2)] = (byte)pPointvalueColor[(j * (Resolution[1] * 3)) + (i * 3 + 0)];  // R
                                canvasScan2[((Resolution[1] - 1 - i) * canvasStride3) + (j * 4 + 1)] = (byte)pPointvalueColor[(j * (Resolution[1] * 3)) + (i * 3 + 1)];  // G
                                canvasScan2[((Resolution[1] - 1 - i) * canvasStride3) + (j * 4 + 0)] = (byte)pPointvalueColor[(j * (Resolution[1] * 3)) + (i * 3 + 2)];  // B
                            }
                        }
                    }
                }

            }

            HeatMap_BitMap2.UnlockBits(canvasData2);
            HeatMap_BitMap2.MakeTransparent(Color.Black);

            //視線履歴
            if (line_bool == true)
            {
                ///ここから視線履歴描画
                Graphics g = Graphics.FromImage(HeatMap_BitMap2);
                //Penオブジェクトの作成(幅5黒色)
                Pen p = new Pen(Color.Black, 3);
                Pen p2 = new Pen(Color.White, 4);


                fixed (int* pDataSet = &CSV_DataXY[0, 0])
                {
                    for (i = 0; i < DataCount - 2; i++)
                    {
                        g.DrawLine(p, pDataSet[i * 2], Resolution[1] - pDataSet[i * 2 + 1], pDataSet[i * 2 + 2], Resolution[1] - pDataSet[i * 2 + 3]);//線描画
                        g.DrawEllipse(p, pDataSet[i * 2] - 6, Resolution[1] - pDataSet[i * 2 + 1] - 5, 10, 10);
                        g.DrawEllipse(p2, pDataSet[i * 2] - 5, Resolution[1] - pDataSet[i * 2 + 1] - 4, 8, 8);
                    }
                }


                p.Dispose();
                g.Dispose();
                ////////////////
            }

            //gazeplot
                Graphics g_gaze = Graphics.FromImage(HeatMap_BitMap2);
                //Penオブジェクトの作成(幅5黒色)
                Pen p_gaze = new Pen(Color.Black, 3);
                Font fnt = new Font("MS UI Gothic", 20);
                //          Pen p2 = new Pen(Color.White, 4);
                int count = 0;
                int r = 10;
                int x = 0;
                int y = 0;
                int flag = 0;
            //gazeplot_bool = true;
            if (gazeplot_bool == true)
            {

                fixed (int* pDataSet = &CSV_DataXY[0, 0])
                {
                    for (i = 0; i < DataCount - 2; i++)
                    {
                        if (i != 0)
                        {
                            //kyorikeisan
                            if (Math.Sqrt((pDataSet[i * 2] - pDataSet[(i - 1) * 2]) * (pDataSet[i * 2] - pDataSet[(i - 1) * 2]) + (pDataSet[i * 2 + 1] - pDataSet[(i - 1) * 2 + 1]) * (pDataSet[i * 2 + 1] - pDataSet[(i - 1) * 2 + 1])) < 3)
                            {
                                if (flag == 0)
                                {
                                    x += pDataSet[(i-1) * 2];
                                    y += pDataSet[(i-1) * 2 + 1];

                                    x += pDataSet[i * 2];
                                    y += pDataSet[i * 2 + 1];
                                    flag += 1;

                                }
                                else
                                {
                                    x += pDataSet[i * 2];
                                    y += pDataSet[i * 2 + 1];
                                }
                                r += 1;
                                flag += 1;
                            }
                            else
                            {
                                //byouga
                                if (flag == 0)
                                {
                                    x = pDataSet[i * 2];
                                    y = pDataSet[i * 2 + 1];
                                }
                                else
                                {
                                    x = x / flag;
                                    y = y / flag;
                                }
                                Pen p2 = new Pen(Color.LightGray, 2);
                                g_gaze.FillEllipse(Brushes.Black, x - ((r+3)/2), Resolution[1] - y - ((r + 3) / 2), r+3, r+3);
                                g_gaze.FillEllipse(Brushes.LightGray, x - (r / 2), Resolution[1] - y- (r/2), r, r);
                                g_gaze.DrawString(count.ToString(), fnt, Brushes.Black, x- count.ToString().Length*2, Resolution[1] - y - 5);
                                r = 10;
                                count += 1;
                                flag = 0;
                                x = 0;
                                y = 0;
                            }
                        }
                    }
                }
            }
            p_gaze.Dispose();
            g_gaze.Dispose();
            /////////


            //変更箇所//
            ///////////////////ここから//////////////////////////

            //string folderPath = System.IO.Directory.GetCurrentDirectory();
            //HeatMap_BitMap.Save(folderPath + "Heatmap.png", System.Drawing.Imaging.ImageFormat.Png);            

            if (heatmap_bool==true)
            {
                HeatMap_BitMap2.Save(System.Reflection.Assembly.GetExecutingAssembly().Location + "/../../../YOUR_RECORD/" + csvName + "_HM.png", System.Drawing.Imaging.ImageFormat.Png);
            }
            else if (line_bool==true)
            {
                HeatMap_BitMap2.Save(System.Reflection.Assembly.GetExecutingAssembly().Location + "/../../../YOUR_RECORD/" + csvName + "_Line.png", System.Drawing.Imaging.ImageFormat.Png);
            }
            else if (gazeplot_bool == true)
            {
                HeatMap_BitMap2.Save(System.Reflection.Assembly.GetExecutingAssembly().Location + "/../../../YOUR_RECORD/" + csvName + "_GP.png", System.Drawing.Imaging.ImageFormat.Png);
            }

            ///////////////////ここまで///////////////////////////
            HeatMap_BitMap2.Dispose();
        }














        unsafe public void Calc(int[,] CSV_DataXY, int[] Resolution, int DataCount, int radius, bool heatmap_bool, bool line_bool, bool gazeplot_bool, string csvName, string playerName)//(CSVの視線データXY,Time,解像度,データの総数,radius,ヒートマップの有無,line bool，ゲイズプロットの有無, 日付, Logフォルダ名)
        {
            LUT = new int[radius * 2 + 1, radius * 2 + 1];
            LUT = LookupTable(LUT,radius);
            int i, j, k;
            int DataSetX;//ポインタ上の座標計算用
            int DataSetY;//ポインタ上の座標計算用
            pointvalue = new float[Resolution[0], Resolution[1]];
            pointvalue_calc = new float[Resolution[0], Resolution[1]];
            PointvalueColor = new float[Resolution[0] * Resolution[1] * 3];//RGB格納配列
            ResolutionX = Resolution[0];
            ResolutionY = Resolution[1];
            fixed (float* pPointvalue = &pointvalue[0, 0])
            {
                fixed (int* pDataSet = &CSV_DataXY[0, 0])
                {
                    fixed (int* pLookUpTable = &LUT[0, 0])
                    {
                        for (i = 0; i < DataCount; i++)
                        {

                            DataSetX = (int)pDataSet[i * 2] * Resolution[1];
                            DataSetY = (int)pDataSet[i * 2 + 1];

                            switch (pDataSet[i * 2] > 0 && pDataSet[i * 2] < Resolution[0] && pDataSet[i * 2 + 1] > 0 && pDataSet[i * 2 + 1] < Resolution[1])
                            {
                                case true:
                                    {
                                        for (j = -radius; j < radius; j++)
                                        {
                                            for (k = -radius; k < radius; k++)
                                            {
                                                if (DataSetY + k > 0 && DataSetY + k < ResolutionY && (int)pDataSet[i * 2] + j > 0 && (int)pDataSet[i * 2] + j < ResolutionX)
                                                {
                                                    pPointvalue[DataSetX + DataSetY + (j * Resolution[1]) + k] += pLookUpTable[(j + radius) + ((k + radius) * (radius * 2 + 1))];//重みづけ
                                                }
                                            }
                                        }
                                        break;
                                    }
                                default:
                                    break;
                            }
                        }
                    }
                }
            }



            int canvasStride2;
            int canvasStride3;

            //////////////////////////////////////////////////////////////////////////////////////////bitmapに色データを移し替える処理
            HeatMap_BitMap = new Bitmap(Resolution[0], Resolution[1], System.Drawing.Imaging.PixelFormat.Format24bppRgb);//canvasを作る
            Bitmap HeatMap_BitMap2 = new Bitmap(Resolution[0], Resolution[1], System.Drawing.Imaging.PixelFormat.Format32bppRgb);


            //bitmapのメモリをロックする
            System.Drawing.Imaging.BitmapData canvasData = HeatMap_BitMap.LockBits(new Rectangle(0, 0, HeatMap_BitMap.Width, HeatMap_BitMap.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, HeatMap_BitMap.PixelFormat);

            System.Drawing.Imaging.BitmapData canvasData2 = HeatMap_BitMap2.LockBits(new Rectangle(0, 0, HeatMap_BitMap2.Width, HeatMap_BitMap2.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, HeatMap_BitMap2.PixelFormat);

            //ポインタを取得
            byte* canvasScan1 = (byte*)canvasData.Scan0.ToPointer();
            //スキャンラインのバイト数

            canvasStride2 = canvasData.Stride;




           //ポインタを取得
           byte* canvasScan2 = (byte*)canvasData2.Scan0.ToPointer();
           //スキャンラインのバイト数

           canvasStride3 = canvasData2.Stride;

            if (heatmap_bool == true)
            {

                fixed (float* pPointvalue = &pointvalue[0, 0])
                {
                    fixed (float* pPointvalueColor = &PointvalueColor[0])
                    {
                        for (i = 0; i < HeatMap_BitMap.Height; i++)
                        {
                            for (j = 0; j < HeatMap_BitMap.Width; j++)
                            {
                                if (pPointvalue[(j * (Resolution[1])) + i] < 0)
                                {
                                    pPointvalueColor[(j * (Resolution[1] * 3)) + (i * 3 + 0)] = 0;//R
                                    pPointvalueColor[(j * (Resolution[1] * 3)) + (i * 3 + 1)] = 0;//G
                                    pPointvalueColor[(j * (Resolution[1] * 3)) + (i * 3 + 2)] = 255;//B
                                }
                                else if (pPointvalue[(j * (Resolution[1])) + i] > 0 && pPointvalue[(j * (Resolution[1])) + i] < 64)//青
                                {
                                    pPointvalueColor[(j * (Resolution[1] * 3)) + (i * 3 + 0)] = 0;//R
                                    pPointvalueColor[(j * (Resolution[1] * 3)) + (i * 3 + 1)] = ((int)pPointvalue[(j * (Resolution[1])) + i] * 4) - 1;//G
                                    pPointvalueColor[(j * (Resolution[1] * 3)) + (i * 3 + 2)] = 255;//B
                                }
                                else if (pPointvalue[(j * (Resolution[1])) + i] >= 64 && pPointvalue[(j * (Resolution[1])) + i] < 128)//青　緑
                                {
                                    pPointvalueColor[(j * (Resolution[1] * 3)) + (i * 3 + 0)] = 0;//R
                                    pPointvalueColor[(j * (Resolution[1] * 3)) + (i * 3 + 1)] = 255;//G
                                    pPointvalueColor[(j * (Resolution[1] * 3)) + (i * 3 + 2)] = (255 - 1 - ((int)pPointvalue[(j * (Resolution[1])) + i] - 64) * 4);//B
                                }
                                else if (pPointvalue[(j * (Resolution[1])) + i] >= 128 && pPointvalue[(j * (Resolution[1])) + i] < 192)//緑 赤
                                {
                                    pPointvalueColor[(j * (Resolution[1] * 3)) + (i * 3 + 0)] = ((int)(pPointvalue[(j * (Resolution[1])) + i] - 128) * 4);//R
                                    pPointvalueColor[(j * (Resolution[1] * 3)) + (i * 3 + 1)] = 255;//G
                                    pPointvalueColor[(j * (Resolution[1] * 3)) + (i * 3 + 2)] = 0;//B
                                }
                                else if (pPointvalue[(j * (Resolution[1])) + i] >= 192 && pPointvalue[(j * (Resolution[1])) + i] < 255)//赤
                                {
                                    pPointvalueColor[(j * (Resolution[1] * 3)) + (i * 3 + 0)] = 255;//R
                                    pPointvalueColor[(j * (Resolution[1] * 3)) + (i * 3 + 1)] = (255 - 1 - ((int)pPointvalue[(j * (Resolution[1])) + i] - 192) * 4);//G
                                    pPointvalueColor[(j * (Resolution[1] * 3)) + (i * 3 + 2)] = 0;//B
                                }
                                else if (pPointvalue[(j * (Resolution[1])) + i] >= 255)
                                {
                                    pPointvalueColor[(j * (Resolution[1] * 3)) + (i * 3 + 0)] = 255;//R
                                    pPointvalueColor[(j * (Resolution[1] * 3)) + (i * 3 + 1)] = 0;//G
                                    pPointvalueColor[(j * (Resolution[1] * 3)) + (i * 3 + 2)] = 0;//B
                                }
                                canvasScan2[((Resolution[1] - 1 - i) * canvasStride3) + (j * 4 + 3)] = 126;  // a
                                canvasScan2[((Resolution[1] - 1 - i) * canvasStride3) + (j * 4 + 2)] = (byte)pPointvalueColor[(j * (Resolution[1] * 3)) + (i * 3 + 0)];  // R
                                canvasScan2[((Resolution[1] - 1 - i) * canvasStride3) + (j * 4 + 1)] = (byte)pPointvalueColor[(j * (Resolution[1] * 3)) + (i * 3 + 1)];  // G
                                canvasScan2[((Resolution[1] - 1 - i) * canvasStride3) + (j * 4 + 0)] = (byte)pPointvalueColor[(j * (Resolution[1] * 3)) + (i * 3 + 2)];  // B
                            }
                        }
                    }
                }

            }

            HeatMap_BitMap2.UnlockBits(canvasData2);
            HeatMap_BitMap2.MakeTransparent(Color.Black);

            //視線履歴
            if (line_bool == true)
            {
                ///ここから視線履歴描画
                Graphics g = Graphics.FromImage(HeatMap_BitMap2);
                //Penオブジェクトの作成(幅5黒色)
                Pen p = new Pen(Color.Black, 3);
                Pen p2 = new Pen(Color.White, 4);


                fixed (int* pDataSet = &CSV_DataXY[0, 0])
                {
                    for (i = 0; i < DataCount - 2; i++)
                    {
                        g.DrawLine(p, pDataSet[i * 2], Resolution[1] - pDataSet[i * 2 + 1], pDataSet[i * 2 + 2], Resolution[1] - pDataSet[i * 2 + 3]);//線描画
                        g.DrawEllipse(p, pDataSet[i * 2] - 6, Resolution[1] - pDataSet[i * 2 + 1] - 5, 10, 10);
                        g.DrawEllipse(p2, pDataSet[i * 2] - 5, Resolution[1] - pDataSet[i * 2 + 1] - 4, 8, 8);
                    }
                }


                p.Dispose();
                g.Dispose();
                ////////////////
            }

            //gazeplot
                Graphics g_gaze = Graphics.FromImage(HeatMap_BitMap2);
                //Penオブジェクトの作成(幅5黒色)
                Pen p_gaze = new Pen(Color.Black, 3);
                Font fnt = new Font("MS UI Gothic", 20);
                //          Pen p2 = new Pen(Color.White, 4);
                int count = 0;
                int r = 10;
                int x = 0;
                int y = 0;
                int flag = 0;
            //gazeplot_bool = true;
            if (gazeplot_bool == true)
            {

                fixed (int* pDataSet = &CSV_DataXY[0, 0])
                {
                    for (i = 0; i < DataCount - 2; i++)
                    {
                        if (i != 0)
                        {
                            //kyorikeisan
                            if (Math.Sqrt((pDataSet[i * 2] - pDataSet[(i - 1) * 2]) * (pDataSet[i * 2] - pDataSet[(i - 1) * 2]) + (pDataSet[i * 2 + 1] - pDataSet[(i - 1) * 2 + 1]) * (pDataSet[i * 2 + 1] - pDataSet[(i - 1) * 2 + 1])) < 3)
                            {
                                if (flag == 0)
                                {
                                    x += pDataSet[(i-1) * 2];
                                    y += pDataSet[(i-1) * 2 + 1];

                                    x += pDataSet[i * 2];
                                    y += pDataSet[i * 2 + 1];
                                    flag += 1;

                                }
                                else
                                {
                                    x += pDataSet[i * 2];
                                    y += pDataSet[i * 2 + 1];
                                }
                                r += 1;
                                flag += 1;
                            }
                            else
                            {
                                //byouga
                                if (flag == 0)
                                {
                                    x = pDataSet[i * 2];
                                    y = pDataSet[i * 2 + 1];
                                }
                                else
                                {
                                    x = x / flag;
                                    y = y / flag;
                                }
                                Pen p2 = new Pen(Color.LightGray, 2);
                                g_gaze.FillEllipse(Brushes.Black, x - ((r+3)/2), Resolution[1] - y - ((r + 3) / 2), r+3, r+3);
                                g_gaze.FillEllipse(Brushes.LightGray, x - (r / 2), Resolution[1] - y- (r/2), r, r);
                                g_gaze.DrawString(count.ToString(), fnt, Brushes.Black, x- count.ToString().Length*2, Resolution[1] - y - 5);
                                r = 10;
                                count += 1;
                                flag = 0;
                                x = 0;
                                y = 0;
                            }
                        }
                    }
                }
            }
            p_gaze.Dispose();
            g_gaze.Dispose();
            /////////


            //変更箇所//
            ///////////////////ここから//////////////////////////

            //string folderPath = System.IO.Directory.GetCurrentDirectory();
            //HeatMap_BitMap.Save(folderPath + "Heatmap.png", System.Drawing.Imaging.ImageFormat.Png);            

            if (heatmap_bool==true)
            {
                HeatMap_BitMap2.Save(System.Reflection.Assembly.GetExecutingAssembly().Location + "/../../../YOUR_RECORD/" + playerName + "/" + csvName + "_HM.png", System.Drawing.Imaging.ImageFormat.Png); // Game00
            }
            else if (line_bool==true)
            {
                HeatMap_BitMap2.Save(System.Reflection.Assembly.GetExecutingAssembly().Location + "/../../../YOUR_RECORD/" + playerName + "/" + csvName + "_Line.png", System.Drawing.Imaging.ImageFormat.Png); // Game00
            }
            else if (gazeplot_bool == true)
            {
                HeatMap_BitMap2.Save(System.Reflection.Assembly.GetExecutingAssembly().Location + "/../../../YOUR_RECORD/" + playerName + "/" + csvName + "_GP.png", System.Drawing.Imaging.ImageFormat.Png); // Game00
            }

            ///////////////////ここまで///////////////////////////
            HeatMap_BitMap2.Dispose();
        }
    }
}