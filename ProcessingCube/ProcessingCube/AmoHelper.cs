using Microsoft.AnalysisServices;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace sh3h.ProcessingCube
{
    public class helper
    {
        private string ConnecteString = string.Empty;
        private Server svr = null;
        public Database db = null;
        public string DBName = string.Empty;
        private static helper _instance = null;

        public static helper Instance
        {
            get
            {
                if (_instance==null)
                {
                    _instance = new helper();
                }
                return _instance;
            }
        }
        public helper()
        {
            try
            {
                ConnecteString = ConfigurationManager.ConnectionStrings["olapStr"].ConnectionString.ToString();
                svr = new Server();
                svr.Connect(ConnecteString);
            }
            catch (Exception e)
            {
                svr = null;
                throw new Exception("无法连接SSAS服务："+e.Message);
            }

        }
        /// <summary>
        /// 确定数据库对象
        /// </summary>
        /// <param name="DBName"></param>
        /// <returns>没有返回null</returns>
        public void FindDBByName(string name)
        {
            db = null;
            DBName = name;
            db = svr.Databases.FindByName(name);
            checkDB();
        }
        /// <summary>
        /// cube对象
        /// </summary>
        /// <param name="db"></param>
        /// <param name="CubeName"></param>
        /// <returns>没有返回null</returns>
        public Cube FindCubeByName(string cubeName)
        {
            checkDB();
            return db.Cubes.FindByName(cubeName);
        }
        /// <summary>
        /// 处理所有维度
        /// </summary>
        /// <param name="db"></param>
        public void UpdateAllDimensions()
        {
            checkDB();
            foreach (Dimension dim in db.Dimensions)
            {
                dim.Process(ProcessType.ProcessUpdate);
            }
        }
        /// <summary>
        /// 单维度处理
        /// </summary>
        /// <param name="db"></param>
        public void UpdateDimension(Dimension dim)
        {
            dim.Process(ProcessType.ProcessUpdate);
        }
        /// <summary>
        /// 单维度处理2
        /// </summary>
        /// <param name="db"></param>
        public void UpdateDimension(string dimName)
        {
            checkDB();
            Dimension dim = db.Dimensions.FindByName(dimName);
            if (dim!=null)
            {
                dim.Process(ProcessType.ProcessUpdate);
            }
            else
            {
                throw new Exception(dimName + "：无法找到该维度");
            }
        }
        /// <summary>
        /// 处理所有cube
        /// </summary>
        /// <param name="db"></param>
        public void UpdateAllCubes()
        {
            checkDB();
            foreach (Cube cube in db.Cubes)
            {
                cube.Process(ProcessType.ProcessFull);
            }
        }
        /// <summary>
        /// 单cube处理
        /// </summary>
        /// <param name="db"></param>
        public void UpdateSingleCube(Cube cube)
        {
            checkDB();
            cube.Process(ProcessType.ProcessFull);

        }
        /// <summary>
        /// 单cube处理2
        /// </summary>
        /// <param name="db"></param>
        public void UpdateSingleCube(string cubeName)
        {
            checkDB();
            Cube cube = FindCubeByName(cubeName);
            if (cube!=null)
            {
                cube.Process(ProcessType.ProcessFull);
            }
            else
            {
                throw new Exception("无法找到对应的cube");
            }
            
        }
        /// <summary>
        /// 处理最新分区-按年月
        /// </summary>
        /// <param name="measureName">度量值组名称</param>
        /// 返回处理的分区名称
        public string UpdatePartitionByMonth(string cubeName,string measureName,string rangSql,int rang,string format)
        {
            //取得当前cube
            Cube cube=FindCubeByName(cubeName);
            Partition part;
            if (cube==null)
            {
                throw new Exception("无法找到对应的cube");
            }
            MeasureGroup mg = cube.MeasureGroups.FindByName(measureName);
            //取最后一个分区
             part = mg.Partitions[mg.Partitions.Count-1];
            
            //是否符合特定格式
            if (!part.Name.Contains("-"))
            {
                throw new Exception(string.Format("最新分区【{0}】：未按照格式进行命名！", part.Name));
            }

            var temp = part.Name.Split('-');
            var lastYearStr = temp[temp.Count() - 1];

            //是否为数字
            if (!Regex.IsMatch(lastYearStr, @"^[+-]?\d*$"))
            {
                throw new Exception(string.Format("最新分区【{0}】：未按照格式进行命名！",part.Name));
            }

            int currentYear= DateTime.Now.Year;
            int lastYear = int.Parse(lastYearStr.Substring(0,4));//当前的分区界线

            part.Process(ProcessType.ProcessFull);

            //对增加的分区数进行控制
            if (currentYear > lastYear && Math.Ceiling(Convert.ToDecimal(currentYear - lastYear) / rang) > 10)
            {
                throw new Exception(string.Format("所需要增加的分区超过10个，请检查分区命名！", cubeName));
            }

            //当前时间不在分区界线内，新增分区
            while (currentYear > lastYear)
            {
                int beginYear = lastYear + 1;   //开始时间
                int endYear = lastYear + rang;  //结束时间
                var parName = string.Format("{0}_{1}-{2}", measureName, beginYear.ToString() + "01", endYear.ToString() + "12");

                if (mg.Partitions.ContainsName(parName))
                {
                    throw new Exception(string.Format("已经存在名为【{0}】的分区,无法继续分区！", part.Name));
                }

                part = mg.Partitions.Add(parName);
                part.StorageMode = StorageMode.Molap;

                //分区语句
                var newrangSql = Formatsql(rangSql, beginYear.ToString(), endYear.ToString(),  format);

                part.Source = new QueryBinding(db.DataSources[0].ID, newrangSql);
                //part.Slice = "[Date].[Calendar Year].&[2001]";
                part.Annotations.Add("lastYear", endYear.ToString());

                lastYear = endYear;   

                cube.Update(UpdateOptions.ExpandFull);
                part.Process(ProcessType.ProcessFull);
            }
            return part.Name;
            
        }
        /// <summary>
        /// 断开连接
        /// </summary>
        public void Disconnect()
        {
            if (svr != null && svr.Connected)
            {
                svr.Disconnect();
            }
        }
        /// <summary>
        /// 检查数据库
        /// </summary>
        public void checkDB()
        {
            if (string.IsNullOrEmpty(DBName))
            {
                throw new Exception("未选择数据库！");
            }
            if (db==null)
            {
                throw new Exception(DBName + "：未找到该数据库");
            }
        }

        /// <summary>
        /// sql语句处理
        /// </summary>
        /// <param name="beginStr"></param>
        /// <param name="endStr"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        private string Formatsql(string rangSql,string beginStr,string endStr,string format)
        {
            string beginYearStr = string.Empty;
            string endYearStr = string.Empty;
            switch (format)
            {
                case "yyyyMM":
                    beginYearStr = beginStr + "01";
                    endYearStr = endStr + "12";
                    break;
                case "yyyyMMdd":
                    beginYearStr = beginStr + "0101";
                    endYearStr = endStr + "1231";
                    break;
                case "yyyyMMddHH":
                    beginYearStr = beginStr + "010100";
                    endYearStr = endStr + "123123";
                    break;
                default:
                    throw new Exception("无法确定日期格式！");
            }
            return rangSql.Replace("@a", beginYearStr).Replace("@b", endYearStr);
        }

    }
}
