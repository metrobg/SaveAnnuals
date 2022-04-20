using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Oracle.ManagedDataAccess.Client;
using System.Globalization;


namespace SaveDocumentsToGMS
{
    public class DBConnection
    {


         string p_user = null;
         string p_password = null;
         string p_host = null;
         string p_port = "1521";
         string p_service_name = null;
         string l_connect_string = null;
        private WardDocumentUpload upload;
        OracleConnection connection;

        public DBConnection(string dbuser, string dbpassword, string host, string database)
        {
            p_user = dbuser;
            p_password = dbpassword;
            p_host = host;
            p_service_name = database;

            Console.WriteLine("host is {0}", p_password);

            try
            {
                string l_data_source = "(DESCRIPTION=(ADDRESS_LIST=" +
            "(ADDRESS=(PROTOCOL=tcp)(HOST=" + p_host + ")" +
            "(PORT=" + p_port + ")))" +
            "(CONNECT_DATA=(SERVICE_NAME=" + p_service_name + ")))";

                l_connect_string = "User Id=" + p_user + ";" +
                   "Password=" + p_password + ";" +
                   "Data Source= " + p_service_name;

                connection = new OracleConnection(l_connect_string);
                connection.Open();
                Console.WriteLine("Connection state is:{0}", connection.State.ToString());
                upload = new WardDocumentUpload();
            }


            catch (Exception e)
            {
                Console.WriteLine("Error establishing database connection {0} ", e.Message.ToString());
                Console.WriteLine("Connection string is:{0}", l_connect_string);
            }
        }
        public OracleConnection getConnection()
        {
            return connection;
        }


        public void dropConnection(OracleConnection connection)
        {
            connection.Close();
            Console.WriteLine("Connection state is:{0}", connection.State.ToString());
        }

        public decimal getNextSequence(OracleConnection connection)
        {
            string strSQL = "SELECT DOCUMENTSEQ.NEXTVAL DOCNUM FROM DUAL";

            OracleCommand cmd = null;
            cmd = new OracleCommand();
            cmd.CommandText = strSQL;
            cmd.Connection = connection;
            OracleDataReader rs = null;
            decimal seq = 0;
            try
            {
                cmd.ExecuteReader();
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
                cmd.ExecuteReader();
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
                Console.WriteLine("Error reading Document Sequence Number {0}", e.ToString());
                rs.Dispose();
                cmd.Dispose();
                connection.Close();
                Environment.Exit(-1);
            }
            rs.Dispose();
            cmd.Dispose();
            return seq;


        }
        public int saveToGMS(string fileName,decimal docnum,decimal wardnum, string sourceFile,OracleConnection connection)
        {
            int rc = 0;
                    
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
                                 
            crNOTES.Value = "Annual Plan, Auto Loaded";           
            crMIMETYPE.Value = "application/pdf";            
            crBASEPATH.Value = decimal.Parse("0");
            cmd.Parameters.Add(crDOCPATH);
           
            crDOCPATH.Value =  destination + sourceFile;

           Console.WriteLine("saving to GMS {0}", sourceFile);
             try {    
                  rc =  cmd.ExecuteNonQuery();
                  upload.saveWardDocument(fileName,@"c:\",sourceFile);
                  Console.WriteLine("From: {0}\t To: {1}", fileName, destination + sourceFile);
                 
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
            
           // File.Delete(fileToProcess);
            return rc;
        }


    }
}
