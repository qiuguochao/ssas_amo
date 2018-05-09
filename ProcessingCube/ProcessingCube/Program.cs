using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using Microsoft.AnalysisServices;
using System.Diagnostics;
using System.Xml;
using System.IO;

namespace sh3h.ProcessingCube
{
    class Program
    {
        
       
        static void Main(string[] args)
        {
            helper hep = null;

            try
            {
                hep = helper.Instance;
            }
            catch (Exception e)
            {
                CommonToos.WriteLogFile(CommonToos.BeginLog()+e.Message+CommonToos.EndLog());
                return;
            }
            
            Stopwatch stopwatch = new Stopwatch();
            Stopwatch total = new Stopwatch();
            StringBuilder logStr = new StringBuilder();

            string pathStr = Path.Combine(System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "configData.xml");
            XmlDocument xmldoc = new XmlDocument();
            xmldoc.Load(pathStr);
            XmlElement root = xmldoc.DocumentElement;
            XmlNodeList dbm = xmldoc.SelectNodes("//db"); 

            if (dbm.Count>0)
            {
                logStr.Append(CommonToos.BeginLog());
                total.Start();
                //处理所有数据库
                foreach (XmlNode item in dbm)
                {
                    try
                    {
                        logStr.Append(CommonToos.DBLog(((XmlElement)item).GetAttribute("name")));
                        //选择当前处理数据库
                        hep.FindDBByName(((XmlElement)item).GetAttribute("name"));

                        //维度处理
                        logStr.Append(CommonToos.appendLine());
                        string dimText = ((XmlElement)item).GetElementsByTagName("dim")[0].InnerText;
                        if (!string.IsNullOrEmpty(dimText))
                        {
                            if (dimText == "*")
                            {
                                //处理所有                              
                                foreach (Dimension ds in hep.db.Dimensions)
                                {
                                    stopwatch.Restart();
                                    hep.UpdateDimension(ds);
                                    logStr.Append(CommonToos.DimOrCubeLog(ds.Name, stopwatch.Elapsed));
                                }
                            }
                            else
                            {
                                string[] dim = dimText.Split(',');
                                foreach (var dname in dim)
                                {
                                    stopwatch.Restart();
                                    hep.UpdateDimension(dname);
                                    logStr.Append(CommonToos.DimOrCubeLog(dname, stopwatch.Elapsed));
                                }

                            }
                        }
                        //cube处理
                        var cubeText = ((XmlElement)item).SelectSingleNode("cubes").InnerText;
                        if (cubeText=="*")
                        {                    
                            foreach (Cube c in hep.db.Cubes)
                            {
                                stopwatch.Restart();
                                hep.UpdateSingleCube(c);
                                logStr.Append(CommonToos.DimOrCubeLog(c.Name, stopwatch.Elapsed));
                            }
                        }
                        else if (!string.IsNullOrEmpty(cubeText))
                        {
                            //处理有分区的cube
                            XmlNodeList cubeList = item.SelectNodes("cubes/cube[@partition='1']");
                            foreach (XmlElement cube in cubeList)
                            {
                                try
                                {
                                    stopwatch.Restart();
                                    string cubeName = cube.GetElementsByTagName("name")[0].InnerText;
                                    string measureName = cube.GetElementsByTagName("measureName")[0].InnerText;
                                    string rangSql = cube.GetElementsByTagName("rangeSql")[0].InnerText;
                                    string format = cube.GetElementsByTagName("format")[0].InnerText;
                                    int rang = int.Parse(cube.GetElementsByTagName("range")[0].InnerText);
                                    string parName=hep.UpdatePartitionByMonth(cubeName, measureName, rangSql, rang, format);

                                    logStr.Append(CommonToos.DimOrCubeLog(string.Format("{0}({1}分区)", cubeName, parName), stopwatch.Elapsed));
                                }
                                catch (Exception e)
                                {
                                    logStr.Append(CommonToos.ErrorLog(cube.GetElementsByTagName("name")[0].InnerText, e.Message));

                                }

                                

                            }
                            //处理没有分区的cube
                            XmlNodeList cube_2 = item.SelectNodes("cubes/cube[@partition='0']");
                            foreach (XmlElement cube in cube_2)
                            {
                                try
                                {
                                    stopwatch.Restart();
                                    string cubeName = cube.GetElementsByTagName("name")[0].InnerText;
                                    hep.UpdateSingleCube(cubeName);
                                    logStr.Append(CommonToos.DimOrCubeLog(cubeName, stopwatch.Elapsed));
                                }
                                catch (Exception e)
                                {
                                    logStr.Append(CommonToos.ErrorLog(cube.GetElementsByTagName("name")[0].InnerText, e.Message));
                                }
                                
                            }
                        }
                        
                        
                    }
                    catch (Exception e)
                    {
                        logStr.Append(e.Message);
                        logStr.Append(CommonToos.appendLine());
                    }
                }
            }
            //断开链接
            hep.Disconnect();
            total.Stop();
            stopwatch.Stop();
            logStr.Append(CommonToos.TotalTime(total.Elapsed));
            logStr.Append(CommonToos.EndLog());

            CommonToos.WriteLogFile(logStr.ToString());
        }

    }
}
