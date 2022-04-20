using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Oracle.ManagedDataAccess.Client;
using System.Security.Permissions;
using System.Security.Principal;

 


namespace SaveDocumentsToGMS
{
    public class DBUtil
    {


         string p_user = null;
         string p_password = null;
         string p_host = null;
         string p_port = "1521";
         string p_service_name = null;
         string l_connect_string = null;
         string StoragePath = null;

        private WardDocumentUpload upload;
        private OracleConnection connection;

        public DBUtil(string dbuser, string dbpassword, string host, string database)
        {
            p_user = dbuser;
            p_password = dbpassword;
            p_host = host;
            p_service_name = database;

            Console.WriteLine("host is {0}", p_host);

            try
            {
                string l_data_source = "(DESCRIPTION=(ADDRESS_LIST=" +
              "(ADDRESS=(PROTOCOL=tcp)(HOST=" + p_host + ")" +
              "(PORT=" + p_port + ")))" +
              "(CONNECT_DATA=(SERVICE_NAME=" + p_service_name + ")))";

                l_connect_string = "User Id=" + p_user + ";" +
                 "Password=" + p_password + ";" +
                 "Data Source= " + l_data_source;

                connection = new OracleConnection(l_connect_string);


                connection.Open();
                Console.WriteLine("Connection state is:{0}", connection.State.ToString());
                //Logger.log("Connection state is: " + connection.State.ToString());
               // StoragePath = getStoragePath(connection);
              //  upload = new WardDocumentUpload();
            }


            catch (Exception e)
            {
                Console.WriteLine("Error establishing database connection {0} ", e.Message.ToString());
                Logger.log("Problem establishing a database connection: " + e.Message.ToString());
                Console.WriteLine("Connection string is:{0}", l_connect_string);
            }
            try
            {
                StoragePath = getStoragePath(connection);
                upload = new WardDocumentUpload();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error getting storage path {0} ", e.Message.ToString());
                Logger.log("Error getting storage path: " + e.Message.ToString());
                Environment.Exit(-1);
            }
        }

        public OracleConnection getConnection()
        {
            return connection;
        }


        public void dropConnection(OracleConnection connection)
        {
            connection.Close();
            Console.WriteLine("Connection state is=>:{0}", connection.State.ToString());
            Logger.log("Database connection closed.");
        }

        public decimal getNextSequence(OracleConnection connection)
        {
            System.Threading.Thread.Sleep(500);
            string strSQL = "SELECT DOCUMENTSEQ.NEXTVAL DOCNUM FROM DUAL";

            OracleCommand cmd = null;
            cmd = new OracleCommand();
            cmd.CommandText = strSQL;
            cmd.Connection = connection;
            
            OracleDataReader rs = null;
            decimal seq = 0;
            try
            {
               // cmd.ExecuteReader();
                rs = cmd.ExecuteReader();
                if (rs.HasRows)
                {
                    while (rs.Read())
                    {
                        seq = rs.GetDecimal(0);

                    }
                }
            }
            catch (OracleException e)
            {
                Console.WriteLine("Error reading Document Sequence Number {0}", e.ToString());
                Logger.log("Error reading Document Sequence Number  exiting." + e.ToString());
                rs.Dispose();
                cmd.Dispose();
                connection.Close();
                Environment.Exit(-1);
            }
            rs.Dispose();
            cmd.Dispose();
            return seq;


        }

        public decimal getBasePath(OracleConnection connection)
        {
            string strSQL = "SELECT INPUTPATH,STORAGEPATH,CURRENTPATHNUM FROM DOCCURRENTPATH,DOCPATH WHERE CURRENTPATHNUM = PATHNUM";

            OracleCommand cmd = null;
            cmd = new OracleCommand();
            cmd.CommandText = strSQL;
            cmd.Connection = connection;
            OracleDataReader rs = null;
            decimal seq = 0;
            try
            {
             
                rs = cmd.ExecuteReader();

                //Console.WriteLine("Count Document Types");
                if (rs.HasRows)
                {
                    while (rs.Read())
                    {
                        seq = rs.GetDecimal(0);

                    }
                }
            }
            catch (OracleException e)
            {
                Console.WriteLine("Error reading Document current path Number {0}", e.ToString());
                Logger.log("Error reading Document current path Number {0}" + e.ToString());
                rs.Dispose();
                cmd.Dispose();
                connection.Close();
                Environment.Exit(-1);
            }
            rs.Dispose();
            cmd.Dispose();
            return seq;


        }

        public string getStoragePath(OracleConnection connection)
        {
            string strSQL = "SELECT STORAGEPATH FROM DOCCURRENTPATH,DOCPATH WHERE CURRENTPATHNUM = PATHNUM";

            OracleCommand cmd = null;
            cmd = new OracleCommand();
            cmd.CommandText = strSQL;
            cmd.Connection = connection;
            OracleDataReader rs = null;
            string _storagePath = null;
            try
            {
               
                rs = cmd.ExecuteReader();
                if (rs.HasRows)
                {
                    while (rs.Read())
                    {
                        _storagePath = rs.GetString(0);

                    }
                }
            }
            catch (OracleException e)
            {
               
                Console.WriteLine("Error reading Document Storage Path {0}", e.ToString());
                Logger.log("Error reading Document Storage Path {0}" + e.ToString());
                rs.Dispose();
                cmd.Dispose();
                connection.Close();
                Environment.Exit(-1);
            }
            rs.Dispose();
            cmd.Dispose();
            return _storagePath;


        }
        public int saveAnnualPlanToGMS(string fileName, decimal docnum, decimal wardnum, string sourceFile, OracleConnection connection)
        {
            int rc = 0;
            string msg = null;

            OracleCommand cmd = null;

            string modTime;
            modTime = File.GetLastWriteTime(fileName).ToString();    // get the document last update time and use as the docdate in the documents table
            DateTime parsedDate = DateTime.Parse(modTime);           // create a date time object from the file update information

            string destination = DateTime.Now.Year.ToString() + "\\" + DateTime.Now.Month.ToString() + "\\" + DateTime.Now.Day.ToString() + "\\";

            /*  SQL insert query for the documents table */
            String insertSQL = "insert into documents (DOCNUM,DOCTYPE,WARDNUM,DOCID,DOCDATE,NOTES,MIMETYPE,BASEPATH,DOCPATH) ";
            insertSQL += "values(:pDOCNUM,:pDOCTYPE,:pWARDNUM,:pDOCID,to_date(:pDOCDATE,'mm/dd/yyyy'),:pNOTES,:pMIMETYPE,:pBASEPATH,:pDOCPATH)";

            cmd = new OracleCommand();
            cmd.CommandText = insertSQL;
            cmd.Connection = connection;

            /*  define Oracle command parameters   */

            OracleParameter crDOCNUM = new OracleParameter("crDOCNUM", OracleDbType.Decimal);
            OracleParameter crDOCTYPE = new OracleParameter("crDOCTYPE", OracleDbType.Varchar2);
            OracleParameter crWARDNUM = new OracleParameter("crWARDNUM", OracleDbType.Decimal);

            OracleParameter crDOCID = new OracleParameter("crDOCID", OracleDbType.Varchar2);
            OracleParameter crDOCDATE = new OracleParameter("crDOCDATE", OracleDbType.Varchar2);


            OracleParameter crNOTES = new OracleParameter("crNOTES", OracleDbType.Varchar2);
            OracleParameter crMIMETYPE = new OracleParameter("crMIMETYPE", OracleDbType.Varchar2);
            OracleParameter crBASEPATH = new OracleParameter("crBASEPATH", OracleDbType.Decimal);
            OracleParameter crDOCPATH = new OracleParameter("crDOCPATH", OracleDbType.Varchar2);

            /* add the parameters to the  Oracle Command */
            cmd.Parameters.Add(crDOCNUM);
            cmd.Parameters.Add(crDOCTYPE);
            cmd.Parameters.Add(crWARDNUM);

            cmd.Parameters.Add(crDOCID);
            cmd.Parameters.Add(crDOCDATE);


            cmd.Parameters.Add(crNOTES);
            cmd.Parameters.Add(crMIMETYPE);
            cmd.Parameters.Add(crBASEPATH);

            /*  set the parameter values and prepare for table insert  */
            crDOCNUM.Value = docnum;
            crDOCTYPE.Value = "ANPLAN";
            crWARDNUM.Value = wardnum;

            crDOCID.Value = "215";
            crDOCDATE.Value = parsedDate.ToShortDateString();

            //crNOTES.Value = "2020 - 2021 Annual Plan Addendum";
            crNOTES.Value = "Annual Plan, Auto Loaded";
            crMIMETYPE.Value = "application/pdf";
            crBASEPATH.Value = decimal.Parse("0");
            cmd.Parameters.Add(crDOCPATH);

            crDOCPATH.Value = destination + sourceFile;


            Console.WriteLine("saving To: {0}", StoragePath + destination + sourceFile);
            try
            {
                msg = upload.saveWardDocument(fileName, StoragePath, sourceFile); // move document to Scanned Documents folder before update of database
                if (msg.Substring(0, 5) != "Error")
                {
                    rc = cmd.ExecuteNonQuery();
                    Console.WriteLine("From: {0}:", fileName);
                }
            }
            catch (OracleException e)
            {
                cmd.Dispose();
                crBASEPATH.Dispose();
                crDOCDATE.Dispose();
                crDOCID.Dispose();
                crDOCNUM.Dispose();
                crDOCPATH.Dispose();
                crDOCTYPE.Dispose();
                crMIMETYPE.Dispose();
                crNOTES.Dispose();
                crWARDNUM.Dispose();
                return rc;

            }

            cmd.Dispose();
            crBASEPATH.Dispose();

            crDOCDATE.Dispose();
            crDOCID.Dispose();
            crDOCNUM.Dispose();
            crDOCPATH.Dispose();
            crDOCTYPE.Dispose();
            crMIMETYPE.Dispose();
            crNOTES.Dispose();
            crWARDNUM.Dispose();

            return rc;
        }
        public int saveAnnualAccountingToGMS(string fileName,decimal docnum,decimal wardnum, string sourceFile,OracleConnection connection)
        {
            int rc = 0;
            string msg = null;
        
            OracleCommand cmd = null;            
            string modTime;
            modTime = File.GetLastWriteTime(fileName).ToString();    // get the document last update time and use as the docdate in the documents table
            DateTime parsedDate = DateTime.Parse(modTime);           // create a date time object from the file update information

            string destination = DateTime.Now.Year.ToString() + "\\" + DateTime.Now.Month.ToString() + "\\"+ DateTime.Now.Day.ToString() + "\\";

           /*  SQL insert query for the documents table */
            String insertSQL = "insert into documents (DOCNUM,DOCTYPE,WARDNUM,DOCID,DOCDATE,NOTES,MIMETYPE,BASEPATH,DOCPATH) ";
            insertSQL += "values(:pDOCNUM,:pDOCTYPE,:pWARDNUM,:pDOCID,to_date(:pDOCDATE,'mm/dd/yyyy'),:pNOTES,:pMIMETYPE,:pBASEPATH,:pDOCPATH)";

            cmd = new OracleCommand();
            cmd.CommandText = insertSQL;
            cmd.Connection = connection;

            /*  define Oracle command parameters   */

            OracleParameter crDOCNUM =    new OracleParameter("crDOCNUM", OracleDbType.Decimal);
            OracleParameter crDOCTYPE =   new OracleParameter("crDOCTYPE", OracleDbType.Varchar2);
            OracleParameter crWARDNUM =   new OracleParameter("crWARDNUM", OracleDbType.Decimal);
           
            OracleParameter crDOCID = new OracleParameter("crDOCID", OracleDbType.Varchar2);
            OracleParameter crDOCDATE = new OracleParameter("crDOCDATE", OracleDbType.Varchar2);
            
             
            OracleParameter crNOTES = new OracleParameter("crNOTES", OracleDbType.Varchar2);
            OracleParameter crMIMETYPE = new OracleParameter("crMIMETYPE", OracleDbType.Varchar2);
            OracleParameter crBASEPATH = new OracleParameter("crBASEPATH", OracleDbType.Decimal);
            OracleParameter crDOCPATH = new OracleParameter("crDOCPATH", OracleDbType.Varchar2);           
           
            /* add the parameters to the  Oracle Command */
            cmd.Parameters.Add(crDOCNUM);
            cmd.Parameters.Add(crDOCTYPE);
            cmd.Parameters.Add(crWARDNUM);
           
            cmd.Parameters.Add(crDOCID);
            cmd.Parameters.Add(crDOCDATE);
            
            
            cmd.Parameters.Add(crNOTES);
            cmd.Parameters.Add(crMIMETYPE);
            cmd.Parameters.Add(crBASEPATH);

            /*  set the parameter values and prepare for table insert  */
            crDOCNUM.Value = docnum;           
            crDOCTYPE.Value = "RETPRP";           
            crWARDNUM.Value = wardnum;
                      
            crDOCID.Value = "214";
            crDOCDATE.Value = parsedDate.ToShortDateString();
                                 
            crNOTES.Value = "Annual Accounting, auto processed";            
            crMIMETYPE.Value = "application/pdf";            
            crBASEPATH.Value = decimal.Parse("0");
            cmd.Parameters.Add(crDOCPATH);
           
            crDOCPATH.Value =  destination + sourceFile;


            Console.WriteLine("saving To: {0}", StoragePath + destination + sourceFile);
             try {
                  msg = upload.saveWardDocument(fileName, StoragePath, sourceFile); // move document to Scanned Documents folder before update of database
                  if (msg.Substring(0, 5) != "Error")
                  {
                      rc = cmd.ExecuteNonQuery();
                      Console.WriteLine("From: {0}:", fileName);
                  }
                  else
                  {
                      Console.WriteLine("Error saving document: {0}", msg);
                      Environment.Exit(-1);
                      
                  }
           }
            catch (OracleException e)
            {
                cmd.Dispose();
                crBASEPATH.Dispose();             
                crDOCDATE.Dispose();
                crDOCID.Dispose();
                crDOCNUM.Dispose();
                crDOCPATH.Dispose();
                crDOCTYPE.Dispose();
                crMIMETYPE.Dispose();
                crNOTES.Dispose();
                crWARDNUM.Dispose();
                return rc;

            }
           
            cmd.Dispose();
            crBASEPATH.Dispose();
           
            crDOCDATE.Dispose();
            crDOCID.Dispose();
            crDOCNUM.Dispose();
            crDOCPATH.Dispose();
            crDOCTYPE.Dispose();
            crMIMETYPE.Dispose();
            crNOTES.Dispose();                   
            crWARDNUM.Dispose();
           
            return rc;
        }


    }
}
