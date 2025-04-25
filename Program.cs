using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using System.Web;
using System.Net;
using System.Net.Security;
using System.Net.Mail;
using System.IO;
using MySql.Data.MySqlClient;
using testJsonBBD;
using System.Configuration;
using System.Security.Cryptography;
using System.Net.Mime;
using System.Collections;

namespace pullCallHistory
{
    internal class Program
    {
        private string logfile;
        static void Main(string[] args)
        {
            Program p = new Program();
            p.createLogFile(0);
            Console.WriteLine("Hello, World!");
            DateTime startTime = DateTime.Now;
            DateTime endTime = DateTime.Now;
            p.logIt("starting now " + DateTime.Now.ToString());
            if(args == null || args.Length == 0)
            {
                //Set dates automatically
                p.logIt("Start auto date!");
                endTime = DateTime.Parse(DateTime.Now.ToString("yyyy-MM-dd HH") + ":00");
                p.logIt("End time should be nearest hour" + endTime.ToString());
                startTime = endTime.AddHours(-2);
                p.logIt("start time = " + startTime.ToString());
                p.logIt("end time = " + endTime.ToString());
            }
            else
            {
                try
                {
                    startTime = DateTime.Parse(args[0]);
                    endTime = DateTime.Parse(args[1]);
                    p.createLogFile(1);
                    p.logIt("Manual Times");
                    startTime = endTime.AddHours(-2);
                    p.logIt("start time = " + startTime.ToString());
                    p.logIt("end time = " + endTime.ToString());

                }
                catch(Exception ex)
                {
                    p.logIt(ex.ToString());
                }


            }
            //p.PunchIt(startTime.ToString("MM-dd-yyyy HH:mm:ss"), endTime.ToString("MM-dd-yyyy HH:mm:ss"));
            p.BBDCallHistJson(startTime.ToString("MM-dd-yyyy HH:mm:ss"), endTime.ToString("MM-dd-yyyy HH:mm:ss"));
            p.RunReports(startTime);
        }
        private string returnXML(string xmlValues, string strTag)
        {
            string pattern = @"<" + strTag + @">(.*)</" + strTag + ">";
            //string match = null;
            Regex rgx = new Regex(pattern, RegexOptions.IgnoreCase);
            Match match = rgx.Match(xmlValues);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            else
            {
                return null;
            }


        }
        private DateTime getStartOfLastMonth(DateTime inputDate)
        {
            string month = "01";
            string day = "01";
            string year = "1";
            if(inputDate.Month == 1)
            {
                //Last year!
                month = "12";
                year = (inputDate.Year - 1).ToString();
            }
            else
            {
                month = (inputDate.Month - 1).ToString();
                year = inputDate.Year.ToString();
            }
            DateTime exportDate;
            try
            {
                exportDate = DateTime.Parse(year + "-" + month + "-" + day);
                
            }
            catch(Exception ex)
            {
                logIt("Datetime conversion failed in private DateTime getStartOfLastMonth");
                exportDate = DateTime.Parse("2020-01-01");
                
            }
            return exportDate;
        }
        private void RunReports(DateTime reportDate)
        {
            
            string dbCS = @"server=10.10.60.23;userid=drpepper;password=M78gV=EN7!F$u!MS;database=TPS";
            
            using (MySqlConnection myCon = new MySqlConnection(dbCS)) {
                myCon.Open();
                DateTime beginOfLMonth = getStartOfLastMonth(reportDate);
                string strSql = "delete FROM TPS.omniCallHistory\r\nwhere call_start_time<'"+ beginOfLMonth.ToString("yyyy-MM-dd") +"'";
                MySqlCommand delCom = new MySqlCommand(strSql, myCon);
                try
                {
                    delCom.ExecuteScalar();
                }
                catch(Exception ex)
                {
                    logIt(ex.Message.ToString());
                    logIt(strSql);
                }
                strSql = "SELECT * FROM TPS.inboundReportsConfiguration where reportTypeID  =7 and active=1";
                MySqlCommand cmd = new MySqlCommand(strSql, myCon);
                string reportId = "";
                try
                {
                    MySqlDataReader reportConfig = cmd.ExecuteReader();
                    while (reportConfig.Read())
                    {
                        //Loop through reports
                        string campIds = reportConfig[13].ToString();
                        reportId = reportConfig[0].ToString();
                        string scheduleId = reportConfig[14].ToString();
                        DispositionData(reportDate,campIds, reportId,dbCS);
                        DistributionDataBuild(reportDate, campIds, reportId,scheduleId,dbCS);
                        ASAstats(reportDate, campIds, reportId, dbCS);
                        

                    }
                    reportConfig.Close();
                }
                catch(Exception ex)
                {
                    logIt(ex.Message.ToString());
                }
                
                MySqlDataReader reportConfig2 = cmd.ExecuteReader();
                while (reportConfig2.Read())
                {
                    createLogFile2(1);
                    logIt("Kicking off a round of reports at " + DateTime.Now.ToString());
                    string campIds = reportConfig2[13].ToString();
                    reportId = reportConfig2[0].ToString();
                    string scheduleId = reportConfig2[14].ToString();
                    string colStartName = GetDayNameAbrv(reportDate) + "StartHr";
                    string colEndName = GetDayNameAbrv(reportDate) + "EndHr";
                    strSql = "Select " + colStartName + ", " + colEndName + " from TPS.inboundReportsSchedule a inner join TPS.inboundReportsConfiguration b on a.scheduleID = b.scheduleID where reportId = " + reportId;
                    logIt(strSql);
                    int intStart = 0;
                    int intEnd = 0;
                    MySqlConnection myCon2 = new MySqlConnection(dbCS);
                    myCon2.Open();
                    try
                    {
                        cmd = new MySqlCommand(strSql, myCon2);
                        MySqlDataReader r = cmd.ExecuteReader();
                        while (r.Read())
                        {
                            intStart = int.Parse(r[colStartName].ToString());
                            intEnd = int.Parse(r[colEndName].ToString());
                        }
                        r.Close();
                    }
                    catch (Exception e)
                    {
                        logIt(e.Message.ToString());
                    }
                    int currentHour = DateTime.Now.Hour;
                    logIt("Current Hour is " + currentHour);
                    logIt("\tStart to End is" + intStart.ToString() + ", " + intEnd.ToString());
                    if (intStart != 0 && intEnd != 0)
                    {
                        if (currentHour < (intEnd + 1))
                        {
                            if (currentHour >= intStart && currentHour <= intEnd)
                            {
                                //is end of day
                                int intEOD = 0;
                                if (reportDate < DateTime.Parse(DateTime.Now.ToString("yyyy-MM-dd")))
                                {
                                    logIt("\tPrior Day Report" + reportDate);
                                    intEOD = 1;
                                }
                                else if (currentHour >= intEnd)
                                {
                                    logIt("\tEnd of Day if (" + currentHour.ToString() + " >= " + intEnd.ToString() + ")");
                                    intEOD = 1;
                                }
                                else
                                {
                                    logIt("\tNormal Report" + intStart.ToString());
                                }
                                if (intEOD == 1)
                                {
                                    strSql = "Select reportid from TPS.inboundReportsConfiguration where reportTypeID  =7 and runendofday = 1 and active=1 and reportid = " + reportId;
                                }
                                else
                                {
                                    strSql = "Select reportid from TPS.inboundReportsConfiguration where reportTypeID  =7 and `runHourly` = 1 and active=1 and reportid = " + reportId;
                                }
                                if (currentHour <= intEnd)
                                {
                                    try
                                    {
                                        cmd = new MySqlCommand(strSql, myCon2);
                                        MySqlDataReader r = cmd.ExecuteReader();
                                        while (r.Read())
                                        {
                                            reportId = r[0].ToString();
                                            string url = @"http://10.10.60.22/cgi-bin/metricsInboundMaster.cgi?runDate=" + reportDate.ToString("yyyy-MM-dd") + @"&repID=" + reportId + @"&timeZone=0&EOD=" + intEOD.ToString() + "&skipdata=0&noEmail=0";
                                            int timeOut = 30000;
                                            if (reportDate.DayOfWeek == DayOfWeek.Sunday)
                                            {
                                                timeOut = 360000;
                                            }

                                            string results = getURL(url, timeOut);
                                            logIt(results);
                                            //Send email!!
                                            string myToLine = returnXML(results, "to");
                                            string myBody = "This is an automated Email.  If you have any questions about this email or would like to stop receiving this email please contact your account manager";
                                            string mySubject = returnXML(results, "subject");
                                            string myFilename = returnXML(results, "emailFileName");
                                            string myFullFilename = returnXML(results, "fullFileName");
                                            //myToLine = myToLine.Replace(",", ";");
                                            myFullFilename = myFullFilename.Replace("/", "\\");
                                            myFullFilename = myFullFilename.Replace("\\home", "\\\\drpepper");
                                            logIt("To = " + myToLine);
                                            logIt("Body = " + myBody);
                                            logIt("Subject = " + mySubject);
                                            logIt("Filename = " + myFilename);
                                            logIt("FullPath =" + myFullFilename);
                                            MailMessage message = new MailMessage("reports@press-one.com", myToLine, mySubject, myBody);
                                            Attachment data = new Attachment(myFullFilename, MediaTypeNames.Application.Octet);
                                            ContentDisposition disposition = data.ContentDisposition;
                                            disposition.CreationDate = System.IO.File.GetCreationTime(myFullFilename);
                                            disposition.ModificationDate = System.IO.File.GetLastWriteTime(myFullFilename);
                                            disposition.ReadDate = System.IO.File.GetLastAccessTime(myFullFilename);
                                            message.Attachments.Add(data);
                                            SmtpClient client = new SmtpClient("mail.frii.com", 587);
                                            client.DeliveryMethod = SmtpDeliveryMethod.Network;
                                            // Add credentials if the SMTP server requires them.

                                            client.EnableSsl = true;
                                            client.Credentials = new NetworkCredential("reports@press-one.com", "G4nd01f!4242");
                                            //client.EnableSsl= true;
                                            int emailSent = 0;
                                            try
                                            {
                                                client.Send(message);
                                                emailSent = 1;



                                            }
                                            catch (Exception ex)
                                            {
                                                logIt(ex.Message);
                                                Console.WriteLine("Exception caught in CreateMessageWithAttachment(): {0}",
                                                    ex.ToString());
                                            }
                                            // Display the values in the ContentDisposition for the attachment.
                                            ContentDisposition cd = data.ContentDisposition;
                                            logIt("Content disposition");
                                            logIt(cd.ToString());
                                            logIt("File {0}" + cd.FileName);
                                            logIt("Size {0}" + cd.Size.ToString());
                                            logIt("Creation {0}" + cd.CreationDate.ToString());
                                            logIt("Modification {0}" + cd.ModificationDate.ToString());
                                            logIt("Read {0}" + cd.ReadDate.ToString());
                                            logIt("Inline {0}" + cd.Inline.ToString());
                                            logIt("Parameters: {0}" + cd.Parameters.Count.ToString());
                                            foreach (DictionaryEntry d in cd.Parameters)
                                            {
                                                logIt("{0} = {1}" + d.Key.ToString() + " " + d.Value.ToString());
                                            }
                                            data.Dispose();
                                            if (emailSent != 0)
                                            {
                                                //Move to archive
                                                string archiveDirectory = "";
                                                if (intEOD == 0)
                                                {
                                                    archiveDirectory = @"\\drpepper\public\reports\inboundMetrics\used\" + myFilename;
                                                }
                                                else
                                                {
                                                    archiveDirectory = @"\\drpepper\public\reports\inboundMetrics\endofday\" + myFilename;
                                                }
                                                if (File.Exists(archiveDirectory))
                                                {
                                                    File.Delete(archiveDirectory);
                                                }
                                                try
                                                {
                                                    File.Move(myFullFilename, archiveDirectory);
                                                }
                                                catch (Exception ex)
                                                {
                                                    logIt("Error in moving file:  Source=" + myFullFilename + " Target = " + archiveDirectory);
                                                    logIt(ex.Message);

                                                }
                                            }


                                            if (reportDate.DayOfWeek == DayOfWeek.Sunday)
                                            {
                                                Thread.Sleep(135000);
                                            }
                                            else
                                            {
                                                Thread.Sleep(45000);
                                            }

                                        }
                                        r.Close();
                                    }
                                    catch (Exception x)
                                    {
                                        logIt(x.Message.ToString());
                                    }
                                }
                            }
                        }
                    }
                }

            }

        }
        private void ASAstats(DateTime reportDate, string camp_Ids, string report_Id, string constring)
        {
            MySqlConnection myConn = new MySqlConnection(constring);
            using (myConn)
            {
                myConn.Open();
                string strSql = "Select count(*) from TPS.omniCallHistory where camp_id in ("+camp_Ids+") and call_start_time >='"+ reportDate.ToString("yyyy-MM-dd") +"' and call_start_time < '"+ reportDate.AddDays(1).ToString("yyyy-MM-dd") +"' and agent_duration>0 and Wait_Duration < 25 ";
                int under25 = Int32.Parse(readOneVal(strSql, myConn));
                strSql = "Select count(*) from TPS.omniCallHistory where camp_id in ("+camp_Ids+") and call_start_time >='"+ reportDate.ToString("yyyy-MM-dd") +"' and call_start_time < '"+ reportDate.AddDays(1).ToString("yyyy-MM-dd") +"' and agent_duration>0 and Wait_Duration < 30 ";
                int under30 = Int32.Parse(readOneVal(strSql, myConn));
                strSql = "Select count(*) from TPS.omniCallHistory where camp_id in ("+camp_Ids+") and call_start_time >='"+ reportDate.ToString("yyyy-MM-dd") +"' and call_start_time < '"+ reportDate.AddDays(1).ToString("yyyy-MM-dd") +"' and agent_duration>0 and Wait_Duration < 45 ";
                int under45 = Int32.Parse(readOneVal(strSql, myConn));
                strSql = "Select count(*) from TPS.omniCallHistory where camp_id in ("+camp_Ids+") and call_start_time >='"+ reportDate.ToString("yyyy-MM-dd") +"' and call_start_time < '"+ reportDate.AddDays(1).ToString("yyyy-MM-dd") +"' and agent_duration>0 and Wait_Duration < 60 ";
                int under60 = Int32.Parse(readOneVal(strSql, myConn));
                strSql = "Select count(*) from TPS.omniCallHistory where camp_id in ("+camp_Ids+") and call_start_time >='"+ reportDate.ToString("yyyy-MM-dd") +"' and call_start_time < '"+ reportDate.AddDays(1).ToString("yyyy-MM-dd") +"' and agent_duration>0";
                int totalCalls = Int32.Parse(readOneVal(strSql, myConn));
                double percent25 = SafePercent(under25, totalCalls);
                double percent30 =  SafePercent(under30, totalCalls);
                double percent45 = SafePercent(under45, totalCalls);
                double percent60 = SafePercent(under60, totalCalls);
                strSql = "delete from TPS.inboundMetricsServiceLevel where reportId = " + report_Id + " and calldate = '" + reportDate.ToString("yyyy-MM-dd") + "'";
                MySqlCommand com = new MySqlCommand(strSql, myConn);
                com.ExecuteScalar();
                strSql="Insert into inboundMetricsServiceLevel (reportID, callDate,answerPercent25,answerPercent30,answerPercent45,answerPercent60, denominator, 25n, 30n, 45n, 60n) "+
                    "Values("+report_Id+", '"+ reportDate.ToString("yyyy-MM-dd") +"', "+ percent25.ToString() +", "+ percent30.ToString() +", "+ percent45.ToString() +", " + percent60.ToString() + ", "+ totalCalls.ToString() +", "+ under25.ToString() +", "+ under30.ToString() +", "+ under45.ToString() +", "+ under60.ToString() +")";

                com = new MySqlCommand(strSql, myConn);
                com.ExecuteScalar();  

            }
        }
        private double SafePercent(int numerator, int denominator)
        {
            try
            {
                return Convert.ToDouble(numerator/denominator);
            }
            catch { return 0; }
        }
        //private void 
        private void DispositionData(DateTime reportDate, string camp_Ids, string report_Id, string conString)
        {
            logIt("Start Disposition");
            using(MySqlConnection myConn = new MySqlConnection(conString))
            {
                myConn.Open();
                string deleteSQL = "Delete FROM TPS.inboundDisposition where reportId = " + report_Id + " and calldate = '" + reportDate.ToString("yyyy-MM-dd") + "'";
                try
                {
                    MySqlCommand cmd = new MySqlCommand(deleteSQL, myConn);
                    cmd.ExecuteScalar();
                }
                catch(Exception ex)
                {
                    logIt(ex.Message.ToString());
                    logIt(deleteSQL);
                }
                deleteSQL = "Delete FROM TPS.inboundAgentDisposition where reportId = " + report_Id + " and calldate = '" + reportDate.ToString("yyyy-MM-dd") + "'";
                try
                {
                    MySqlCommand cmd = new MySqlCommand(deleteSQL, myConn);
                    cmd.ExecuteScalar();
                }
                catch(Exception ex)
                {
                    logIt(ex.Message.ToString());
                    logIt(deleteSQL);
                }
                //Update status group etc
                string strSql = "update TPS.omniCallHistory a inner join (Select * from TPS.Snowfly where statusGroup>200) b on a.disposition_id = b.statusdetail set a.statusGroup = b.statusGroup, a.statusCode = b.statusCode, a.statusDetail = b.statusDetail where (a.statusGroup is null or a.statusGroup =0) and disposition_id>0;";
                try
                {
                    MySqlCommand cmd = new MySqlCommand(strSql,myConn);
                    cmd.ExecuteScalar();
                }
                catch(Exception ex)
                {
                    logIt(ex.Message.ToString());
                    logIt(strSql);
                }
                //Update Abandons
                strSql = "Update TPS.omniCallHistory set statusGroup = 49, statusCode = 1, statusDetail = 2 where call_status = 'ABANDONED' and call_type != 'BLITRANSFER' and hangup_by= 'USER' and Wait_Duration>30 and (statusGroup = 0 or statusGroup is null);";
                try
                {
                    MySqlCommand cmd = new MySqlCommand(strSql,myConn);
                    cmd.ExecuteScalar();
                }
                catch(Exception ex)
                {
                    logIt(ex.Message.ToString());
                    logIt(strSql);
                }
                //Add more special status here
                //****************************
                //Uncoded calls
                strSql = "Update TPS.omniCallHistory set statusGroup = 48, statusCode = 1, statusDetail = 1 where agent_duration>0 and statusGroup = 0;";
                try
                {
                    MySqlCommand cmd = new MySqlCommand(strSql,myConn);
                    cmd.ExecuteScalar();
                }
                catch(Exception ex)
                {
                    logIt(ex.Message.ToString());
                    logIt(strSql);
                }
                //IVR calls
                strSql = "Update TPS.omniCallHistory set statusGroup = 49, statusCode = 1, statusDetail = 1 where call_status = 'ANSWERED' and wait_duration=0;";
                try
                {
                    MySqlCommand cmd = new MySqlCommand(strSql,myConn);
                    cmd.ExecuteScalar();
                }
                catch(Exception ex)
                {
                    logIt(ex.Message.ToString());
                    logIt(strSql);
                }
                //Insert into inbound dispos
                //strSql = "Insert into TPS.inboundDisposition (`reportID`,`callDate`,`statusGroup`,`statusCode`,`statusDetail`,`count`,`talkTime`,`wrapTime`,`waitTime`)" 
                //    +" (SELECT "+ report_Id +",'"+ reportDate.ToString("yyyy-MM-dd") +"',b.statusGroup, b.statusCode,b.statusDetail, count(*) as calls,sum(talk_duration) as talk, sum(ACW_Duration) as wrap, sum(wait_duration) as wait FROM TPS.omniCallHistory a inner join TPS.Snowfly b on a.disposition_id = b.statusDetail" 
                //    +" where b.statusGroup>=200 and a.`camp_id` in("+ camp_Ids +") and a.`call_start_time`>='"+ reportDate.ToString("yyyy-MM-dd") +"' and a.`call_start_time`< '"+ reportDate.AddDays(1).ToString("yyyy-MM-dd") +"' group by disposition_name, b.statusText, b.statusCode,b.statusGroup,b.statusDetail)";
                strSql = "Insert into TPS.inboundDisposition (`reportID`,`callDate`,`statusGroup`,`statusCode`,`statusDetail`,`count`,`talkTime`,`wrapTime`,`waitTime`)" 
                    + " (SELECT "+ report_Id +",'"+ reportDate.ToString("yyyy-MM-dd") +"',b.statusGroup, b.statusCode,b.statusDetail, count(*) as calls,sum(talk_duration) as talk, sum(ACW_Duration) as wrap, sum(wait_duration) as wait FROM TPS.omniCallHistory a inner join TPS.Snowfly b on a.statusGroup = b.statusGroup and a.statusCode = b.statusCode and a.statusDetail = b.statusDetail"
                    +" where a.`camp_id` in("+ camp_Ids +") and a.`call_start_time`>='"+reportDate.ToString("yyyy-MM-dd") +"' and a.`call_start_time`< '"+ reportDate.AddDays(1).ToString("yyyy-MM-dd") +"' group by  b.statusCode,b.statusGroup,b.statusDetail)";


                logIt("DispositionData = " +strSql);
                try
                {
                    MySqlCommand cmd = new MySqlCommand(strSql,myConn);
                    cmd.ExecuteScalar();
                }
                catch(Exception ex)
                {
                    logIt(ex.Message.ToString());
                    logIt(strSql);
                }
                //strSql = "Insert into TPS.inboundDisposition (`reportID`,`callDate`,`statusGroup`,`statusCode`,`statusDetail`,`count`,`talkTime`,`wrapTime`,`waitTime`)" 
                //    +" (SELECT "+ report_Id +",'"+ reportDate.ToString("yyyy-MM-dd") +"',49,1,2, count(*) as calls,sum(talk_duration) as talk, sum(ACW_Duration) as wrap, sum(wait_duration) as wait FROM TPS.omniCallHistory a " 
                //    +" where a.`camp_id` in("+ camp_Ids +") and a.`call_start_time`>='"+ reportDate.ToString("yyyy-MM-dd") +"' and a.`call_start_time`< '"+ reportDate.AddDays(1).ToString("yyyy-MM-dd") +"' and call_status = 'ABANDONED' and wait_duration >=30)";

                //try
                //{
                //    MySqlCommand cmd = new MySqlCommand(strSql,myConn);
                //    cmd.ExecuteScalar();
                //}
                //catch(Exception ex)
                //{
                //    logIt(ex.Message.ToString());
                //    logIt(strSql);
                //}
                //strSql = "insert into TPS.inboundAgentDisposition " +
                //        "(`reportID`,`agentid`,`callDate`,`statusGroup`,`statusCode`,`statusDetail`,`count`,`talkTime`,`wrapTime`,`waitTime`) " +
                //        "(SELECT "+ report_Id +", '"+ reportDate.ToString("yyyy-MM-dd") +"' ,b.statusGroup, b.statusCode,b.statusDetail, count(*) as calls,sum(talk_duration) as talk, sum(ACW_Duration) as wrap, sum(wait_duration) as wait FROM TPS.omniCallHistory a inner join TPS.Snowfly b on a.disposition_id = b.statusDetail " +
                //        "where b.statusGroup>=200 and a.`camp_id` in("+ camp_Ids +") and a.`call_start_time`>='"+ reportDate.ToString("yyyy-MM-dd") +"' and a.`call_start_time`< '"+ reportDate.AddDays(1).ToString("yyyy-MM-dd") +"'" +
                //        "group by disposition_name, b.statusText, b.statusCode,b.statusGroup,b.statusDetail,a.agent_username)";
                strSql = "insert into TPS.inboundAgentDisposition " +
                        "(`reportID`,`agentid`,`callDate`,`statusGroup`,`statusCode`,`statusDetail`,`count`,`talkTime`,`wrapTime`,`waitTime`) " +
                        "SELECT " + report_Id + ",a.agent_username, '" + reportDate.ToString("yyyy-MM-dd") + "' ,b.statusGroup, b.statusCode,b.statusDetail, count(*) as calls,sum(talk_duration) as talk, sum(ACW_Duration) as wrap, sum(wait_duration) as wait " +
                        "FROM TPS.omniCallHistory a inner join TPS.Snowfly b on a.statusGroup = b.statusGroup and a.statusCode = b.statusCode and a.statusDetail = b.statusDetail " +
                        "where a.`camp_id` in(" + camp_Ids + ") and a.`call_start_time`>='" + reportDate.ToString("yyyy-MM-dd") + "' and a.`call_start_time`< '" + reportDate.AddDays(1).ToString("yyyy-MM-dd") + "' and a.statusgroup not in (0,49) group by  b.statusCode,b.statusGroup,b.statusDetail,a.agent_username";
                logIt("Agent insert = "+strSql);
               try
                {
                    MySqlCommand cmd = new MySqlCommand(strSql,myConn);
                    cmd.ExecuteScalar();
                }
                catch(Exception ex)
                {
                    logIt(ex.Message.ToString());
                    logIt(strSql);
                }
            }
        }
        private string getURL(string url, int intTimeOut)
        {
            //create web client
            DateTime mystart = DateTime.Now;
            DateTime myend;
            //WebRequest mywr = WebRequest.Create(url);// assign url
            HttpWebRequest mywr = (HttpWebRequest)WebRequest.Create(url);
            mywr.Proxy = null;
            mywr.ServicePoint.ConnectionLimit = 500;
            mywr.MaximumAutomaticRedirections = 4;
            mywr.MaximumResponseHeadersLength = 4;
            
            int myTimeout = intTimeOut;
            mywr.Timeout = myTimeout; //Time out in miliseconds
            mywr.Method = "get";
            WebResponse wr = null;
            //logIt("This is the url request" + url);
            string returnedString = null;
            try
            {
                //get uri
                wr = mywr.GetResponse();
                StreamReader sr = new StreamReader(wr.GetResponseStream());
                returnedString = sr.ReadToEnd();
                wr.Close();
                sr.Close();

            }
            catch (WebException ex)
            {
                wr = null;
                mywr = null;
                if (ex.Status == WebExceptionStatus.Timeout)
                {
                    myend = DateTime.Now;
                    logIt("TimedOut! Timeout set to " + myTimeout.ToString() + " " + (myend-mystart).TotalMilliseconds.ToString() +  " Miliseconds Elasped");
                    return "Error=999";
                }
                else
                {
                    myend = DateTime.Now;
                    logIt(ex.Message + " " + url + " " + (myend - mystart).TotalMilliseconds.ToString() + " Miliseconds Elasped");
                    return "Error=992"; //web status error write actual error to log

                }
            }
            myend = DateTime.Now;
            logIt("Success " + url + " " + (myend - mystart).TotalMilliseconds.ToString() + " Miliseconds Elasped");
            if (returnedString != null)
            {
                wr = null;
                mywr = null;
                //logIt(returnedString);
                return returnedString;
            }
            else
            {
                wr = null;
                mywr = null;
                return null;
            }
        }
        private void DistributionDataBuild(DateTime reportDate, string Camp_Ids, string Report_Id, string Schedule_Id, string conString)
        {
            logIt("Starting Distribution");
            using (MySqlConnection MyConn = new MySqlConnection(conString))
            {
                MyConn.Open();

                string colNameStart = GetDayNameAbrv(reportDate) + "StartHr";
                string colNameEnd = GetDayNameAbrv(reportDate) + "EndHr";
                string reportStartDate = reportDate.ToString("yyyy-MM-dd") + " 00:00";
                string reportEndDate = reportDate.AddDays(1).ToString("yyyy-MM-dd") + " 00:00";

                string deleteSql = "Delete from TPS.inboundDistribution where reportid = " + Report_Id + " and datetimestart >='" + reportStartDate + "' and datetimeend<'" + reportEndDate + "'";
                try
                {
                    MySqlCommand com = new MySqlCommand(deleteSql, MyConn);
                    com.ExecuteScalar();
                }
                catch(Exception ex)
                {
                    logIt(ex.Message.ToString());
                    logIt(deleteSql);
                }
                string strSql = "Select `" + colNameStart + "`,`" + colNameEnd + "` from TPS.inboundReportsSchedule where scheduleId=" + Schedule_Id;
                try
                {
                    MySqlCommand cmd = new MySqlCommand(strSql, MyConn);
                    MySqlDataReader mySched = cmd.ExecuteReader();
                    while (mySched.Read())
                    {
                        int startHour = int.Parse(mySched[0].ToString());
                        int endHour = int.Parse(mySched[1].ToString());
                        for (int i = startHour; i <= endHour; i++)
                        {
                            //Top of the hour
                            DateTime startDate = DateTime.Parse(reportDate.ToString("yyyy-MM-dd") + " " + LPadZero(i.ToString(), 2) + ":00");
                            DateTime endDate = DateTime.Parse(reportDate.ToString("yyyy-MM-dd") + " " + LPadZero(i.ToString(), 2) + ":30");
                            logIt("***********Top of Hour****************");
                            logIt(startDate.ToString("yyyy-MM-dd HH:mm") + " To " + endDate.ToString("yyyy-MM-dd HH:mm"));
                            logIt("**************************************");
                            HalfHourBlockData(Camp_Ids, startDate.ToString("yyyy-MM-dd HH:mm"), endDate.ToString("yyyy-MM-dd HH:mm"), Report_Id, conString);

                            //bottom of the hour
                            startDate = DateTime.Parse(reportDate.ToString("yyyy-MM-dd") + " " + LPadZero(i.ToString(), 2) + ":30");
                            endDate = DateTime.Parse(reportDate.AddHours(1).ToString("yyyy-MM-dd") + " " + LPadZero((i+1).ToString(), 2) + ":00");
                            logIt("***********Bottom of Hour****************");
                            logIt(startDate.ToString("yyyy-MM-dd HH:mm") + " To " + endDate.ToString("yyyy-MM-dd HH:mm"));
                            logIt("**************************************");
                            HalfHourBlockData(Camp_Ids, startDate.ToString("yyyy-MM-dd HH:mm"), endDate.ToString("yyyy-MM-dd HH:mm"), Report_Id, conString);



                        }

                    }
                    mySched.Close();
                }
                catch (Exception ex)
                {
                    logIt(ex.Message.ToString());
                }
            }

        }
        private void HalfHourBlockData(string CampIds, string startTime, string endTime, string reportId, string StrConn)
        {
            logIt("******************************HalfHourBlockData*****************************************************");
            logIt("StartTime = " + startTime);
            logIt("EndTime = " + endTime);
            logIt("***********************************************************************************");
            using (MySqlConnection conn = new MySqlConnection(StrConn))
            {
                conn.Open();
                
                string strWhere = "where camp_id in (" + CampIds + ") and `call_start_time`>= '" + startTime + "' and `call_start_time`< '" + endTime + "'";
                logIt("\tWhere = " + strWhere);
                string strSql = "select count(*), sum(call_duration), sum(agent_duration), sum(acw_duration),sum(wait_duration), max(wait_duration) from TPS.omniCallHistory " + strWhere;
                try
                {
                    logIt("\tBigBlockQuery=" +strSql);
                    MySqlCommand cmd = new MySqlCommand(strSql, conn);
                    MySqlDataReader blockRd = cmd.ExecuteReader();
                    string strCalls = "";
                    string strDur = "";
                    string strAgentDur = "";
                    string strAcwDur = "";
                    string strWaitDur = "";
                    string strMaxWait = "";
                    while (blockRd.Read())
                    {
                         strCalls = blockRd[0].ToString();
                         strDur = blockRd[1].ToString();
                         strAgentDur = blockRd[2].ToString();
                         strAcwDur = blockRd[3].ToString();
                         strWaitDur = blockRd[4].ToString();
                         strMaxWait = blockRd[5].ToString();
                    }
                    blockRd.Close();
                    //Get abandons
                    strSql = "select count(*) from TPS.omniCallHistory " + strWhere + " and call_status = 'Abandoned' and hangup_by= 'USER' and call_type != 'BLITRANSFER'";
                    string strAbandons = readOneVal(strSql, conn);
                    strSql = "select count(*) from TPS.omniCallHistory " + strWhere + " and call_status = 'Abandoned' and wait_duration>=30 and hangup_by= 'USER' and call_type != 'BLITRANSFER'";
                    logIt("Abandon Query" + strSql);
                    
                    string strCountedAbandons = readOneVal(strSql, conn);
                    logIt("Abandon count = " + strCountedAbandons);
                    strSql = "select sum(wait_duration) from TPS.omniCallHistory " + strWhere + " and call_status = 'Abandoned' and wait_duration>=30 and hangup_by= 'USER' and call_type != 'BLITRANSFER'";
                    string strAbandonTime = readOneVal(strSql, conn);
                    strSql = "select count(*) from TPS.omniCallHistory " + strWhere + " and callback_type !='';";
                    string strCallBacks = readOneVal(strSql, conn);
                    strSql = "Select count(*) from (Select agent_username from TPS.omniCallHistory " + strWhere + " and agent_duration >0 group by agent_username) as A";
                    string strAgents = readOneVal(strSql, conn);
                    strSql = "Select count(*) from TPS.omniCallHistory " + strWhere + " and statusgroup not in (0,49)";
                    string strAgentCalls = readOneVal(strSql, conn);
                    if (strCalls == "")
                    {
                        strCalls = "0";

                    }
                    if(strDur== "") { strDur= "0";}
                    if(strAgentDur== "") { strAgentDur= "0";}
                    if(strAcwDur== "") { strAcwDur = "0"; }
                    if(strWaitDur== "") { strWaitDur= "0";}
                    if(strAbandons== "") { strAbandons= "0";}
                    if(strCountedAbandons== "") { strCountedAbandons= "0";}
                    if(strAbandonTime== "") { strAbandonTime= "0";}   
                    if(strCallBacks== "") { strCallBacks= "0";}
                    if(strAgents== "") { strAgents= "0";}
                    if(strAgentCalls== "") { strAgentCalls= "0";}
                    if (strMaxWait == "") { strMaxWait= "0";}
                    strSql = "INSERT INTO `TPS`.`inboundDistribution`(`dateTimeStart`,`dateTimeEnd`,`reportID`,`totalACW`,`totalTalk`,`maxWait`,`totalWait`,`totalWaitOptOut`,`countOptOuts`,`abandons`,`totalAbandonTime`,`countAgents`,`agentCalls`,`callBackRequested`,`callBackAttempts`,`callbackComplete`,`totalInbound`,`agentTime`,`callsUnder30`,`callsOver30`,`callBackSched`,`callBackMade`,`callBackAttempted`,`callBackFirstAttNumerator`,`callBackFirstAttDenominator`,`medianTalk`,`medianAcw`,`medianWait`,`medianTotalTalk`,`medianTotalAcw`,`medianTotalWait`)" +
                                                            "VALUES('" + startTime + "','" + endTime + "'," + reportId + "," + strAcwDur + "," + strAgentDur + "," + strMaxWait + "," + strWaitDur + ",0," + strCallBacks + "," + strCountedAbandons + "," + strAbandonTime + "," + strAgents + "," + strAgentCalls + "," + strCallBacks + "," + strCallBacks + "," + strCallBacks + "," + strCalls + ",0,0,0," + strCallBacks + "," + strCallBacks + "," + strCallBacks + ",0,0,0,0,0,0,0,0); ";
                    logIt("Block Query=" +strSql);
                    cmd = new MySqlCommand(strSql, conn);
                    cmd.ExecuteScalar();
                    //blockRd.Close();
                }
                catch (Exception ex)
                {
                    logIt(ex.Message.ToString());
                }
            }

        }
        private string LPadZero(string str, int count)
        {
            if (str.Length < count)
            {
                while (str.Length < count)
                {
                    str = "0"+str;
                }
                return str;
            }
            else
            {
                return str;
            }
        }
        private string GetDayNameAbrv(DateTime ADate)
        {
            string dayOfWeek = "";
            switch (ADate.DayOfWeek)
            {
                case DayOfWeek.Monday: 
                    dayOfWeek = "Mon";
                    break;
                case DayOfWeek.Tuesday: 
                    dayOfWeek = "Tues";
                    break;
                case DayOfWeek.Wednesday: 
                    dayOfWeek = "Wed";
                    break;
                case DayOfWeek.Thursday:
                    dayOfWeek = "Thurs";
                    break;
                case DayOfWeek.Friday: 
                    dayOfWeek = "Fri";
                    break;
                case DayOfWeek.Saturday: 
                    dayOfWeek = "Sat";
                    break;
                case DayOfWeek.Sunday: 
                    dayOfWeek = "Sun";
                    break;

            }
            return dayOfWeek;
        }
        private void logIt(string text)
        {
            StreamWriter rw = new StreamWriter(logfile, true);
            rw.WriteLine(text);
            rw.Close();
        }
        private void createLogFile(int debugmode)
        {
            DateTime dt = DateTime.Now;
            if (debugmode == 1)
            {
                logfile = @"F:\Logs\Hist\log" + dt.ToString("yyyyMMdd") + ".txt";
            }
            else
            {
                logfile = @"F:\Logs\Hist\DEBUGlog" + dt.ToString("yyyyMMdd") + ".txt";
            }
        }
        private void createLogFile2(int debugmode)
        {
            DateTime dt = DateTime.Now;
            if (debugmode == 0)
            {
                logfile = @"F:\Logs\Hist\Reportlog" + dt.ToString("yyyyMMdd") + ".txt";
            }
            else
            {
                logfile = @"F:\Logs\Hist\DebugReportlog" + dt.ToString("yyyyMMdd") + ".txt";
            }
        }
        private int sendJsonHttp(string url, string strData, ref string Output)

        {
            DateTime startDate = DateTime.Now;
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
            HttpWebRequest mywr = (HttpWebRequest)HttpWebRequest.Create(url);
            int myTimeout = 30000;
            mywr.Timeout = myTimeout; //Time out in miliseconds
            mywr.Method = "POST";
            mywr.ContentType = "application/json";
            try
            {
                //wc.Headers[HttpRequestHeader.ContentType] = "application/json";

                //string response = wc.UploadString(url, "POST", strData);
                using (var streamWriter = new StreamWriter(mywr.GetRequestStream()))
                {
                    streamWriter.Write(strData);
                    streamWriter.Flush();
                    streamWriter.Close();
                }
                var myResponse = (HttpWebResponse)mywr.GetResponse();
                using (var streamReader = new StreamReader(myResponse.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();
                    //logIt(result);
                    Output = result;
                }
                return 200;
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.ProtocolError)
                {
                    var response = ex.Response as HttpWebResponse;
                    logIt("HTTP Status Code: " + (int)response.StatusCode);
                    logIt("Response Description:" + response.StatusDescription);
                    int statusCode = 0;

                    statusCode = (int)response.StatusCode;

                    return statusCode;
                }
                else
                {
                    logIt("Non protocol error " + ex.Message);
                    return 999;
                }
            }

        }
        private string formatDate(string strDateTime)
        {
            try
            {
                DateTime mydate = DateTime.Parse(strDateTime);
                string format = "yyyy-MM-dd HH:mm:ss";
                return mydate.ToString(format);
            }
            catch (Exception ex)
            {
                logIt("bad date");
                return "00-00-00 00:00:00";
            }
        }
        private int stringTimeToInt(string aTime)
        {
            if (aTime != null)
            {
                string[] times = aTime.Split(':');
                if (times.Count() == 3)
                {
                    int hour = int.Parse(times[0]);
                    int minute = int.Parse(times[1]);
                    int second = int.Parse(times[2]);
                    second = second + (minute * 60) + (hour * 3600);
                    return second;
                }
                else
                {
                    logIt("Wrong Time format" + aTime);
                    return 0;
                }
            }
            else
            {
                logIt("Null Time!");
                return 0;
            }
        }
        private void PunchIt(string TimeStart, string TimeEnd)
        {
            string dbCS = @"server=10.10.60.23;userid=drpepper;password=M78gV=EN7!F$u!MS;database=TPS";
            string url = "https://omni.bbdtel.com/HoduCC_api/v1.4/getDispositionDetails";
            string json = @"{""token"":""OIvw1AerfOtAuWkj"",""tenant_id"":""1003"",""start_time"":"""+ TimeStart + @""",""end_time"":""" + TimeEnd + @"""}";
            using (MySqlConnection con = new MySqlConnection(dbCS))
            {
                con.Open();
                logIt(url);
                logIt(json);
                string resultString = "";
                int resultCode= sendJsonHttp(url, json, ref resultString);
                if (resultCode == 200)
                {
                    logIt(resultString);
                    DispositionDetails dp = JsonConvert.DeserializeObject<DispositionDetails>(resultString);
                    logIt(dp.getRows().ToString());
                    foreach (DetailItems d in dp.Result)
                    {
                        string lineItem = d.Acd_name + " " + d.Agt_name + " " + d.Call_id + " " + d.Phone_number;
                        url = "https://omni.bbdtel.com/HoduCC_api/v1.4/getCallDetails";
                        //json = @"{""token"":""OIvw1AerfOtAuWkj"",""tenant_id"":""1003"",""call_id"":" + d.Call_id + @"""}";
                        //json = @"{""call_id"":" + d.Call_id + @""",""cg_id"":"""",""token"":""OIvw1AerfOtAuWkj"",""tenant_id"":""1003"",}";
                        json = @"{

                    ""call_id"": """ + d.Call_id + @""",

                    ""token"": ""OIvw1AerfOtAuWkj"",

                    ""tenant_id"": ""1003""

                  }";
                        //logIt(url);
                        //logIt(json);
                        logIt(sendJsonHttp(url, json, ref resultString).ToString());
                        //logIt(resultString);
                        //logIt(resultString);
                        //CallDetails cd = JsonConvert.DeserializeObject<CallDetails>(resultString);
                        //callResult callr = JsonConvert.DeserializeObject<callResult>(resultString);
                        RootHistory callRoot = JsonConvert.DeserializeObject<RootHistory>(resultString);
                        //cdSacallRoot.resultifice sacallRoot.resultifice = JsonConvert.DeserializeObject<cdSacallRoot.resultifice>(resultString);
                        //string input = sacallRoot.resultifice.Result;
                        //callResult callRoot.result1 = JsonConvert.DeserializeObject<callResult>(input);
                        //cd.Result.Add(callRoot.result1);
                        if (callRoot != null)
                        {

                            int acwTime = int.Parse(GetDateDiff(callRoot.result.call_hangup_time, callRoot.result.call_disposition_time));
                            if (acwTime < 0) { acwTime= 0; }
                            int talkTime = int.Parse(GetDateDiff(callRoot.result.call_agent_time,callRoot.result.call_hangup_time));
                            if (talkTime < 0) { talkTime= 0; }
                            string sql = @"INSERT INTO `TPS`.`omniCallHistory`
                            (`call_id`,
                            `phone_number`,
                            `call_type`,
                            `did_id`,
                            `trunk_id`,
                            `rule_id`,
                            `agt_id`,
                            `acd_id`,
                            `camp_id`,
                            `call_start_time`,
                            `call_queue_time`,
                            `call_agent_time`,
                            `call_hangup_time`,
                            `call_disposition_time`,
                            `disposition_id`,
                            `call_status`,
                            `hangup_reason`,
                            `hangup_by`,
                            `call_duration`,
                            `agent_duration`,
                            `queue_name`,
                            `campagin_name`,
                            `trunk_name`,
                            `rule_name`,
                            `agent_username`,
                            `cg_id`,
                            `cust_name`,
                            `disposition_name`,
                            `callback_type`,
                            `transfer_id`,
                            `geo_location`,
                            `call_answer_time`,
                            `answred_duration`,
                            `recording_path`,
                            `ACW_Duration`,
                            `Wait_Duration`,
                            `Talk_Duration`)
                            VALUES
                            ( " + callRoot.result.call_id.ToString() + @" ,
                             '" + callRoot.result.phone_number + @"',
                             '" + callRoot.result.call_type + @"' ,
                             " + callRoot.result.did_id.ToString() + @" ,
                             " + callRoot.result.trunk_id.ToString() + @",
                             " + callRoot.result.rule_id.ToString() + @" ,
                             " + callRoot.result.agt_id + @",
                             " + callRoot.result.acd_id + @",
                             " + callRoot.result.camp_id + @" ,
                             '" + formatDate(callRoot.result.call_start_time) + @"' ,
                             '" + formatDate(callRoot.result.call_queue_time) + @"' ,
                             '" + formatDate(callRoot.result.call_agent_time) + @"',
                             '" + formatDate(callRoot.result.call_hangup_time) + @"',
                            '" + formatDate(callRoot.result.call_disposition_time) + @"' ,
                             " + callRoot.result.disposition_id + @" ,
                             '" + callRoot.result.call_status + @"',
                             '" + callRoot.result.hangup_reason + @"',
                             '" + callRoot.result.hangup_by + @"',
                             " + stringTimeToInt(callRoot.result.call_duration) + @",
                             " + stringTimeToInt(callRoot.result.agent_duration) + @",
                             '" + callRoot.result.queue_name + @"',
                             '" + callRoot.result.campagin_name + @"',
                             '" + callRoot.result.trunk_name + @"',
                             '" + callRoot.result.rule_name + @"',
                             '" + callRoot.result.agent_username + @"',
                             " + callRoot.result.cg_id + @" ,
                             '" + callRoot.result.cust_name + @"',
                             '" + callRoot.result.disposition_name + @"',
                             '" + callRoot.result.callback_type + @"',
                             " + callRoot.result.cg_id + @",
                             '" + callRoot.result.geo_location + @"',
                            '" + formatDate(callRoot.result.call_answer_time) + @"' ,
                             " + stringTimeToInt(callRoot.result.answred_duration) + @" ,
                             '" + callRoot.result.recording_path + @"',
                             " + acwTime.ToString() + @",
                             " + GetDateDiff(callRoot.result.call_queue_time, callRoot.result.call_agent_time) + "," +
                             talkTime.ToString() +@" );";
                            string deleteSQL = "Delete from `TPS`.`omniCallHistory` where call_id = " + callRoot.result.call_id.ToString();
                            try
                            {
                                MySqlCommand com = new MySqlCommand(deleteSQL,con);
                                com.ExecuteScalar();
                            }
                            catch(Exception ex)
                            {
                                logIt(ex.Message.ToString());
                                logIt(deleteSQL);
                            }
                            //logIt(sql);
                            try
                            {
                                MySqlCommand cmd = new MySqlCommand(sql, con);
                                cmd.ExecuteScalar();

                            }
                            catch (Exception ex)
                            {
                                logIt(sql);
                                logIt(ex.Message);
                            }


                        }


                    }
                }
                else
                {
                    logIt("Not successful: " + resultCode.ToString());
                }
            }


        }
        private void BBDCallHistJson(string TimeStart, string TimeEnd)
        {
            string dbCS = @"server=10.10.60.23;userid=drpepper;password=M78gV=EN7!F$u!MS;database=TPS";
            string url = "https://omni.bbdtel.com/HoduCC_api/v1.4/getCallHistory";
            //string json = @"{""token"":""OIvw1AerfOtAuWkj"",""tenant_id"":""1003"",""start_time"":"""+ TimeStart + @""",""end_time"":""" + TimeEnd + @"""}";
            string json = @"{
                    ""token"" : ""OIvw1AerfOtAuWkj"",
                    ""search_on_time"" : ""START"",
                    ""search_type"" : ""DATETIME"",
                    ""start_datetime"" : """+ TimeStart +@""", 
                    ""end_datetime"" : """+ TimeEnd +@""",   
                    ""tenant_id"" : ""1003"" 
                }";
            using (MySqlConnection con = new MySqlConnection(dbCS))
            {
                con.Open();
                logIt(url);
                logIt(json);
                string resultString = "";
                int resultCode= sendJsonHttp(url, json, ref resultString);
                if (resultCode == 200)
                {
                    logIt(resultString);
                    string matchPattern = @"""call_id"":(\d*),";
                    Match[] matches = Regex.Matches(resultString, matchPattern).Cast<Match>().ToArray();

                    //string matchReplace = @"""CallDetail"":";
                    //string fixedResult = Regex.Replace(resultString, matchPattern, matchReplace);
                    //RootCallHist ch = JsonConvert.DeserializeObject<RootCallHist>(resultString);
                    //logIt(ch.result.total.ToString());
                    //for (int i = 0;i<ch.result.total;i++)
                    foreach(Match m in matches)
                    {
                        url = "https://omni.bbdtel.com/HoduCC_api/v1.4/getCallDetails";
                        //json = @"{""token"":""OIvw1AerfOtAuWkj"",""tenant_id"":""1003"",""call_id"":" + d.Call_id + @"""}";
                        //json = @"{""call_id"":" + d.Call_id + @""",""cg_id"":"""",""token"":""OIvw1AerfOtAuWkj"",""tenant_id"":""1003"",}";
                        json = @"{

                    ""call_id"": """ + m.Groups[1].Value + @""",

                    ""token"": ""OIvw1AerfOtAuWkj"",

                    ""tenant_id"": ""1003""

                  }";
                        logIt(sendJsonHttp(url, json, ref resultString).ToString());
                        logIt(resultString);
                        RootHistory callRoot = JsonConvert.DeserializeObject<RootHistory>(resultString);
                        if (callRoot != null)
                        {

                            int acwTime = int.Parse(GetDateDiff(callRoot.result.call_hangup_time, callRoot.result.call_disposition_time));
                            if (acwTime < 0) { acwTime= 0; }
                            int talkTime = int.Parse(GetDateDiff(callRoot.result.call_agent_time,callRoot.result.call_hangup_time));
                            if (talkTime < 0) { talkTime= 0; }
                            int waitTime = 0;
                            if(callRoot.result.call_status == "ABANDONED")
                            {
                                waitTime = int.Parse( GetDateDiff(callRoot.result.call_queue_time, callRoot.result.call_hangup_time));
                            }
                            else
                            {
                                waitTime = int.Parse( GetDateDiff(callRoot.result.call_queue_time, callRoot.result.call_agent_time));
                            }
                            int agent_duration = stringTimeToInt(callRoot.result.agent_duration);
                            if(callRoot.result.call_type == "EXTTRANSFER")
                            {
                                agent_duration = 0;
                            }
                            string sql = @"INSERT INTO `TPS`.`omniCallHistory`
                            (`call_id`,
                            `phone_number`,
                            `call_type`,
                            `did_id`,
                            `trunk_id`,
                            `rule_id`,
                            `agt_id`,
                            `acd_id`,
                            `camp_id`,
                            `call_start_time`,
                            `call_queue_time`,
                            `call_agent_time`,
                            `call_hangup_time`,
                            `call_disposition_time`,
                            `disposition_id`,
                            `call_status`,
                            `hangup_reason`,
                            `hangup_by`,
                            `call_duration`,
                            `agent_duration`,
                            `queue_name`,
                            `campagin_name`,
                            `trunk_name`,
                            `rule_name`,
                            `agent_username`,
                            `cg_id`,
                            `cust_name`,
                            `disposition_name`,
                            `callback_type`,
                            `transfer_id`,
                            `geo_location`,
                            `call_answer_time`,
                            `answred_duration`,
                            `recording_path`,
                            `ACW_Duration`,
                            `Wait_Duration`,
                            `Talk_Duration`)
                            VALUES
                            ( " + callRoot.result.call_id.ToString() + @" ,
                             '" + callRoot.result.phone_number + @"',
                             '" + callRoot.result.call_type + @"' ,
                             " + callRoot.result.did_id.ToString() + @" ,
                             " + callRoot.result.trunk_id.ToString() + @",
                             " + callRoot.result.rule_id.ToString() + @" ,
                             " + callRoot.result.agt_id + @",
                             " + callRoot.result.acd_id + @",
                             " + callRoot.result.camp_id + @" ,
                             '" + formatDate(callRoot.result.call_start_time) + @"' ,
                             '" + formatDate(callRoot.result.call_queue_time) + @"' ,
                             '" + formatDate(callRoot.result.call_agent_time) + @"',
                             '" + formatDate(callRoot.result.call_hangup_time) + @"',
                            '" + formatDate(callRoot.result.call_disposition_time) + @"' ,
                             " + callRoot.result.disposition_id + @" ,
                             '" + callRoot.result.call_status + @"',
                             '" + callRoot.result.hangup_reason + @"',
                             '" + callRoot.result.hangup_by + @"',
                             " + stringTimeToInt(callRoot.result.call_duration) + @",
                             " + agent_duration.ToString() + @",
                             '" + callRoot.result.queue_name + @"',
                             '" + callRoot.result.campagin_name + @"',
                             '" + callRoot.result.trunk_name + @"',
                             '" + callRoot.result.rule_name + @"',
                             '" + CheckUserName(callRoot.result.agent_username) + @"',
                             " + callRoot.result.cg_id + @" ,
                             '" + callRoot.result.cust_name + @"',
                             '" + callRoot.result.disposition_name + @"',
                             '" + callRoot.result.callback_type + @"',
                             " + callRoot.result.cg_id + @",
                             '" + callRoot.result.geo_location + @"',
                            '" + formatDate(callRoot.result.call_answer_time) + @"' ,
                             " + stringTimeToInt(callRoot.result.answred_duration) + @" ,
                             '" + callRoot.result.recording_path + @"',
                             " + acwTime.ToString() + @",
                             " + waitTime + "," +
                             talkTime.ToString() +@" );";
                            string deleteSQL = "Delete from `TPS`.`omniCallHistory` where call_id = " + callRoot.result.call_id.ToString();
                            try
                            {
                                MySqlCommand com = new MySqlCommand(deleteSQL,con);
                                com.ExecuteScalar();
                            }
                            catch(Exception ex)
                            {
                                logIt(ex.Message.ToString());
                                logIt(deleteSQL);
                            }
                            //logIt(sql);
                            try
                            {
                                MySqlCommand cmd = new MySqlCommand(sql, con);
                                cmd.ExecuteScalar();

                            }
                            catch (Exception ex)
                            {
                                logIt(sql);
                                logIt(ex.Message);
                            }


                        }

                    }
                    //eliminate agent time for exteranl transfers
                    //try
                    //{
                    //    string mySql = "Update `TPS`.`omniCallHistory` set agent_duration = 0 where call_start_time >= '" +
                    //    MySqlCommand myCom
                    //}
                    
                }
                else
                {
                    logIt("Not successful: " + resultCode.ToString());
                }
            }


        }
        private string CheckUserName( string AName)
        {
            if (AName!= null)
            {
                try
                {
                    return int.Parse(AName).ToString();
                }
                catch
                {
                    logIt("Username " + AName + " is non numeric");
                    return "100";
                }
            }
            else
            {
                return "";
            }
        }
        private string GetDateDiff(string startDateTime, string endDateTime)
        {
            
            try
            {
                DateTime start = DateTime.Parse(startDateTime);
                DateTime end = DateTime.Parse(endDateTime);
                TimeSpan ts = end - start;
                return ts.TotalSeconds.ToString();
            }
            catch(Exception ex)
            {
                logIt(ex.Message);
                return "0";
            }
            



        }
        private void deleteRecord(string CallId, MySqlConnection myConn)
        {
            string strSql = "Select count(*) from TPS.omniCallHistory where call_id = " + CallId;
            if (int.Parse(readOneVal(strSql, myConn)) > 1)
            {
                strSql = "Delete from TPS.omniCallHistory where call_id = " + CallId;
                try
                {
                    MySqlCommand cmd = new MySqlCommand(strSql, myConn);
                    logIt(cmd.ExecuteScalar().ToString());

                }
                catch (Exception ex)
                {
                    logIt(ex.Message);
                }
            }
        }
        private string readOneVal(string strSql, MySqlConnection con)
        {
            MySqlCommand cmd = new MySqlCommand(strSql, con);
            MySqlDataReader reader = cmd.ExecuteReader();
            string Result = "";
            while (reader.Read())
            {
                Result = reader[0].ToString();
            }
            reader.Close();
            return Result;
        }
    }
}