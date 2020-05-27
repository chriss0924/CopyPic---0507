using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using InxGeneral;
using System.Threading;
using System.Reflection;
using System.Data.SqlClient;

namespace CopyPic
{
    public partial class PM21 : Form
    {

        #region "參數"

        #region "DB參數"
        // DB connect string 
        public string con = "Data Source=193.74.0.150;Initial Catalog=CoilDb;User ID=steinb;Password=12345";

        // 1st command without iImage
        public string strcmd_1 = "SELECT DISTINCT [CoilId] FROM [CoilDb].[steinb].[Defects] where [CoilId] = 39505";

        //public string strcmd_2 = "SELECT [CoilId],[DefectId],[Class],[CameraNo] FROM [CoilDb].[steinb].[Defects] where CoilID = ";

        #endregion

        #region "object"
        //SqlCommand sqlcmd = new SqlCommand();
        //Byte[] buffer;
        DataTable dt = new DataTable("ID");
        //DateTime time_start;
        //DateTime time_end;
        #endregion

        #region "DB欄位轉換類別"
        object obj_iClass;
        object obj_iCameraNO;
        object obj_iCoilID;
        object obj_iDefect;

        #endregion

        #region "資料夾參數"
        string sourcePath = "";
        string destPath = "";
        string defectType = "";
        #endregion

        #region "字串預設參數"
        string strDBFileName = "";
        string strClass = "";
        string serverName = "P206427";
        string strSRCING = "srcimg";
        string strCoilID = "";
        string strDefect = "";
        string strCameraNO = "";
        #endregion

        #endregion

        public PM21()
        {
            InitializeComponent();
        }

        #region "Control Method"

        private void btnClear_Click(object sender, EventArgs e)
        {
            txtDestPath.Clear();
            txtSourcePath.Clear();
        }

        private void btnSelectData_Click(object sender, EventArgs e)
        {
            txtSourcePath.Text = @"C:\test_source";
            txtDestPath.Text = @"C:\test_dest";
            sql();
            MessageBox.Show("分類完成");
        }

        #endregion

        #region "method"

        public void sql()
        {
            sourcePath = txtSourcePath.Text + "\\";
            destPath = txtDestPath.Text + "\\";
            // 1. db connect
            using (SqlConnection cn = new SqlConnection(con))
            {
                // 2. open DB
                cn.Open();
              
                DirectoryInfo di = new DirectoryInfo(@"C:\test_source\00039XXX\");
                DirectoryInfo[] diArr = di.GetDirectories();

                foreach (DirectoryInfo dri in diArr)
                {
                    //LogWrite(dri.Name); //列出所有目錄清單
                    string strFolderName = dri.Name;

                    string[] str = strFolderName.Split('0');
                    strFolderName = str[3].ToString();
                    strFolderName = "'" + strFolderName + "'";

                    //strcmd_2 = strcmd_2 + textBox1.Text;  //把textbox1用目錄取代
                    string strcmd_2 = "SELECT [CoilId],[DefectId],[Class],[CameraNo] FROM [CoilDb].[steinb].[Defects] where CoilID = ";
                    strcmd_2 = strcmd_2 + strFolderName;

                    // 3. SQL command
                    using (SqlCommand cmd_2 = new SqlCommand(strcmd_2, cn))
                    {
                        //先把資料秀出來
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd_2))
                        {
                            //建立dataSet or dataTable class by Fill method
                            dt = new DataTable();
                            da.Fill(dt);
                            dgvData.DataSource = dt;
                            dgvData.Update();
                        }

                        //4.SQLdataReader 去抓欄位物件
                        using (SqlDataReader dr = cmd_2.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                // 5.判斷資料是否為空
                                if (!dr[0].Equals(DBNull.Value))
                                {
                                    // 6.取db物件並重新分配
                                    //db欄位 CoilId(int),DefectId(int),Class(int),CameraNo(int) obj_int
                                    obj_iCoilID = dr[0];
                                    Int32 iCoilID = (Int32)obj_iCoilID;
                                    strCoilID = iCoilID.ToString().PadLeft(8, '0');

                                    obj_iDefect = dr[1];
                                    Int32 iDefectID = (Int32)obj_iDefect;
                                    strDefect = iDefectID.ToString().PadLeft(4, '0');

                                    obj_iClass = dr[2];
                                    Int32 iClass = (Int32)obj_iClass;
                                    strClass = iClass.ToString().PadLeft(2, '0'); //資料庫裡Class為defect分類，利用這個來建立分類資料夾

                                    obj_iCameraNO = dr[3];
                                    Int32 iCameraNO = (Int32)obj_iCameraNO;
                                    strCameraNO = iCameraNO.ToString().PadLeft(2, '0');

                                    //組字串
                                    //strFileName由資料庫欄位組合成的，之後利用這字串去搜尋目標資料夾內的檔案並複製到新的資料夾
                                    strDBFileName = serverName + "_" + strCoilID + "_" + strCameraNO + "_" + strSRCING + "_" + strDefect;
                                    LogWrite("DB :" + strDBFileName);
                                    //比對資料
                                    switch (strClass)
                                    {
                                        case "20":
                                        case "21":
                                        case "22":
                                        case "23":
                                        case "30":
                                        case "31":
                                        case "32":
                                        case "33":
                                        case "50":
                                        case "51":
                                        case "52":
                                        case "53":
                                        case "60":
                                        case "70":
                                        case "81":
                                        case "82":
                                        case "182":
                                            compaer_string();
                                            break;
                                        default:
                                            break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            //MessageBox.Show("分類完成");
        }

        /// <summary>
        /// 第二次分解字串 + 鎖定搜尋範圍 + 找到後複製圖片過去
        /// </summary>
        public void compaer_string()
        {
            //P206427-00039494-03-SRCING-0001
            //strFileName += ".jpg"; 
            strDBFileName += ".tif";
            string[] dirs = Directory.GetFiles(sourcePath, strDBFileName);

            // strfilename = P206427_00039494_03_srcimg_0001.tif
            string[] a = strDBFileName.Split('_');
            string str1st_ID = a[1].Substring(3,2).ToString();
            str1st_ID = "*" + str1st_ID + "*"; //*37*
            string str2nd_ID = a[1].ToString(); // 00037997
            string str3th_camera = a[2].ToString(); //相機

            //另外處理流水號
            string[] b = a[4].ToString().Split('.');
            string str4th_sn = b[0].ToString();
            str4th_sn = str4th_sn.PadLeft(8, '0');
            str4th_sn = str4th_sn.Substring(3, 2);
            //LogWrite("yyyyy is :" + str4th_sn);

            DirectoryInfo root = new DirectoryInfo(sourcePath);
            //P206427_00039650_03_srcimg_0001.tif
            //第一層 0003-97xxxx --> "37"才是重點
            foreach (DirectoryInfo subDir1 in root.GetDirectories(str1st_ID))
            {
                //第二層 00039650
                foreach(DirectoryInfo subDir2 in subDir1.GetDirectories(str2nd_ID))
                {
                    //string j = subDir2.FullName;

                    //string[] filePaths = Directory.GetFiles(subDir2.FullName, strDBFileName, SearchOption.AllDirectories);

                    //folder exist or not
                    //copy file
                    //var v = destPath + strClass;
                    //if (Directory.Exists(v))
                    //{
                    //    File.Copy(filePaths.ToString(), v, true);
                    //}
                    //else
                    //{
                    //    Directory.CreateDirectory(destPath + strClass);
                    //    File.Copy(filePaths.ToString(), v, true);
                    //}
                    //第三層 相機
                    foreach (DirectoryInfo subDir3 in subDir2.GetDirectories(str3th_camera))
                    {
                        try
                        {
                            string[] filePaths = Directory.GetFiles(subDir3.FullName, strDBFileName, SearchOption.AllDirectories);
                            string sourcePaths = filePaths[0].ToString();
                            var v = destPath + strClass;
                            var destination = Path.Combine(destPath + strClass, strDBFileName);

                            if (Directory.Exists(v))
                            {

                                File.Copy(sourcePaths, destination, true);
                            }
                            else
                            {
                                Directory.CreateDirectory(destPath + strClass);

                                File.Copy(sourcePaths, destination, true);
                            }
                        }
                        catch(Exception ex)
                        {
                            LogWrite("subDir3 : " + sourcePath);
                        }
                        //    //第四層 流水號 00002xxx --> "2"才是重點

                        //    string uu = str4th_sn.Substring(0, 1);
                        //    string u2 = str4th_sn.Substring(1, 1);
                        //    if (str4th_sn == "00")
                        //    {
                        //        #region "000 00 xxx "
                        //        string yy = "000" + str4th_sn + "XXX"; // 000 00 XXX

                        //        foreach (DirectoryInfo subDir4 in subDir3.GetDirectories(yy))
                        //        {
                        //            if (Directory.Exists(destPath + strClass))
                        //            {
                        //                var source = subDir4.FullName + "\\" + strDBFileName;
                        //                var destination = Path.Combine(destPath + strClass, strDBFileName);

                        //                try
                        //                {
                        //                    File.Copy(source, destination, true);
                        //                }
                        //                catch (Exception ex)
                        //                {
                        //                    LogWrite(source.ToString());
                        //                }
                        //            }
                        //            else
                        //            {
                        //                //create dir
                        //                Directory.CreateDirectory(destPath + strClass);
                        //                //字串搜尋 -> 使用strFileName去找 source 資料夾內的檔案並複製到 dest 資料夾
                        //                var source = subDir4.FullName + "\\" + strDBFileName;
                        //                var destination = Path.Combine(destPath + strClass, strDBFileName);

                        //                try
                        //                {
                        //                    File.Copy(source, destination, true);
                        //                }
                        //                catch (Exception ex)
                        //                {
                        //                    LogWrite(source.ToString());
                        //                }
                        //            }
                        //        }

                        //        #endregion
                        //    }

                        //    else if (str4th_sn == "01")
                        //    {
                        //        #region " 000 01 xxx "

                        //        string yy = "000" + str4th_sn + "XXX";

                        //        foreach (DirectoryInfo subDir4 in subDir3.GetDirectories(yy))
                        //        {
                        //            if (Directory.Exists(destPath + strClass))
                        //            {
                        //                var source = subDir4.FullName + "\\" + strDBFileName;
                        //                var destination = Path.Combine(destPath + strClass, strDBFileName);

                        //                try
                        //                {
                        //                    File.Copy(source, destination, true);
                        //                }
                        //                catch (Exception ex)
                        //                {
                        //                    LogWrite(source.ToString());
                        //                }
                        //            }
                        //            else
                        //            {
                        //                //create dir
                        //                Directory.CreateDirectory(destPath + strClass);
                        //                //字串搜尋 -> 使用strFileName去找 source 資料夾內的檔案並複製到 dest 資料夾
                        //                var source = subDir4.FullName + "\\" + strDBFileName;
                        //                var destination = Path.Combine(destPath + strClass, strDBFileName);

                        //                try
                        //                {
                        //                    File.Copy(source, destination, true);
                        //                }
                        //                catch (Exception ex)
                        //                {
                        //                    LogWrite(source.ToString());
                        //                }
                        //            }
                        //        }
                        //        #endregion
                        //    }

                        //    else if (str4th_sn == "10")
                        //    {
                        //        #region " 000 10 xxx "

                        //        string yy = "000" + str4th_sn + "XXX"; //000 1/2/3 *                           

                        //        foreach (DirectoryInfo subDir4 in subDir3.GetDirectories(yy))
                        //        {
                        //            //MessageBox.Show("subDir1 is : " + subDir4.ToString());
                        //            if (Directory.Exists(destPath + strClass))
                        //            {
                        //                var source = subDir4.FullName + "\\" + strDBFileName;
                        //                var destination = Path.Combine(destPath + strClass, strDBFileName);

                        //                try
                        //                {
                        //                    File.Copy(source, destination, true);
                        //                }
                        //                catch (Exception ex)
                        //                {
                        //                    LogWrite(source.ToString());
                        //                }
                        //            }
                        //            else
                        //            {
                        //                //create dir
                        //                Directory.CreateDirectory(destPath + strClass);
                        //                //字串搜尋 -> 使用strFileName去找 source 資料夾內的檔案並複製到 dest 資料夾
                        //                var source = subDir4.FullName + "\\" + strDBFileName;
                        //                var destination = Path.Combine(destPath + strClass, strDBFileName);

                        //                try
                        //                {
                        //                    File.Copy(source, destination, true);
                        //                }
                        //                catch (Exception ex)
                        //                {
                        //                    LogWrite(source.ToString());
                        //                }
                        //            }
                        //        }
                        //        #endregion
                        //    }             
                    }
                }
            }
        }

        private void CreateFolder(int iField,string sourcePath , string destPath)
        {

            int i = iField;
            string source = sourcePath;
            string dest = destPath;


            if (i == 20)
                defectType = GetDescriptionText(eDefectList.Little_Broken_hole.ToString());
            if (i == 21)
                defectType = GetDescriptionText(eDefectList.Mid_Broken_hole.ToString());
            if (i == 22)
                defectType = GetDescriptionText(eDefectList.Big_Broken_hole.ToString());
            if (i == 23)
                defectType = GetDescriptionText(eDefectList.Super_Big_Broken_hole.ToString());
            if (i == 30)
                defectType = GetDescriptionText(eDefectList.Little_Transparent.ToString());
            if (i == 31)
                defectType = GetDescriptionText(eDefectList.Mid_Transparent.ToString());
            if (i == 32)
                defectType = GetDescriptionText(eDefectList.Big_Transparent.ToString());
            if (i == 33)
                defectType = GetDescriptionText(eDefectList.Super_Big_Transparent.ToString());
            if (i == 50)
                defectType = GetDescriptionText(eDefectList.Little_Black_stain.ToString());
            if (i == 51)
                defectType = GetDescriptionText(eDefectList.Mid_Black_stain.ToString());
            if (i == 52)
                defectType = GetDescriptionText(eDefectList.Big_Black_stain.ToString());
            if (i == 53)
                defectType = GetDescriptionText(eDefectList.Super_Big_Black_stain.ToString());
            if (i == 60)
                defectType = GetDescriptionText(eDefectList.Edge_Crack.ToString());
            if (i == 70)
                defectType = GetDescriptionText(eDefectList.Wrinkle.ToString());
            if (i == 81)
                defectType = GetDescriptionText(eDefectList.Bright_Streak.ToString());
            if (i == 82)
                defectType = GetDescriptionText(eDefectList.Dark_Streak.ToString());
            if (i == 182)
                defectType = GetDescriptionText(eDefectList.Smile.ToString());


        }


        /// <summary>
        /// log內涵 date、time、message 
        /// </summary>
        /// <param name="message"></param>
        public void LogWrite(string message)
        {
            {
                string DIRNAME = @"C:\Project\FilterImage_AI\Log\";
                string FILENAME = DIRNAME + DateTime.Now.ToString("yyyyMMdd") + ".txt";

                if (!Directory.Exists(DIRNAME))
                    Directory.CreateDirectory(DIRNAME);

                if (!File.Exists(FILENAME))
                {
                    File.Create(FILENAME).Close();
                }
                using (StreamWriter sw = File.AppendText(FILENAME))
                {
                    sw.Write("Log Entry : ");
                    sw.WriteLine("{0}\t:\t{1}", DateTime.Now.ToString("HH:mm:ss.fff"), message);
                }
            }
        }

        public enum eDefectList
        {
            [Description("小破孔")]
            Little_Broken_hole = 20,

            [Description("中破孔")]
            Mid_Broken_hole = 21,

            [Description("大破孔")]
            Big_Broken_hole = 22,

            [Description("超大破孔")]
            Super_Big_Broken_hole = 23,

            //Transparent
            [Description("小透明點")]
            Little_Transparent = 30,

            [Description("中透明點")]
            Mid_Transparent = 31,

            [Description("大透明點")]
            Big_Transparent = 32,

            [Description("超大透明點")]
            Super_Big_Transparent = 33,

            [Description("小汙點")]
            Little_Black_stain = 50,

            [Description("中汙點")]
            Mid_Black_stain = 51,

            [Description("大汙點")]
            Big_Black_stain = 52,

            [Description("超大汙點")]
            Super_Big_Black_stain = 53,

            [Description("破邊")]
            Edge_Crack = 60,

            [Description("皺紋")]
            Wrinkle = 70,

            [Description("白刀痕")]
            Bright_Streak = 81,

            [Description("黑刀痕")]
            Dark_Streak = 82,

            [Description("Smile")]
            Smile = 182,
        }

        public static string GetDescriptionText(string value)
        {
            Type type = typeof(eDefectList);
            var name = Enum.GetNames(type).Where(f => f.Equals(value, StringComparison.CurrentCultureIgnoreCase)).Select(d => d).FirstOrDefault();

            // 找無相對應的列舉
            if (name == null)
            {
                return string.Empty;
            }

            // 利用反射找出相對應的欄位
            var field = type.GetField(name);
            // 取得欄位設定DescriptionAttribute的值
            var customAttribute = field.GetCustomAttributes(typeof(DescriptionAttribute), false);

            // 無設定Description Attribute, 回傳Enum欄位名稱
            if (customAttribute == null || customAttribute.Length == 0)
            {
                return name;
            }

            // 回傳Description Attribute的設定
            return ((DescriptionAttribute)customAttribute[0]).Description;
        }

        #endregion



    }
}
