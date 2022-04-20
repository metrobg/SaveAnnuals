using System;
using System.IO;

using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SaveDocumentsToGMS
{
    class Logger
    {
        /** 
         * Log file path 
         *  
         * @var string 
         */
        private static string __log_file_path;

        /** 
         * __log_file_path get/set 
         */
        public static string filePath
        {
            get { return Logger.__log_file_path; }
            set { if (value.Length > 0) Logger.__log_file_path = value; }
        }

        /** 
         * Flush log file contents 
         *  
         * @return void 
         */
        public static void flush()
        {
            File.WriteAllText(Logger.filePath, string.Empty);
        }

        /** 
         * Log message 
         *  
         * @param string msg 
         * @return void 
         */
        public static void log(string msg)
        {
            if (msg.Length > 0)
            {
                using (StreamWriter sw = File.AppendText(Logger.filePath))
                {
                    sw.WriteLine("{0} {1}: {2}", DateTime.Now.ToShortDateString(), DateTime.Now.ToShortTimeString(), msg);
                    sw.Flush();
                }
            }
        }
    } 



}
