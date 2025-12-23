using Opc2Lib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HansDebuggerApp.OPC
{
    public class PlcSettings
    {
        public string Address = "172.18.34.57";
        public int Port = 4840;
        public int Timeout = 10000;
        public LogicControllerUaClient.SecurityPolicies Policy = LogicControllerUaClient.SecurityPolicies.None;
        public string Login = "guiopc";
        public string Password = "1";
        public string VarSpace = "|var|PLC210 OPC-UA.Application.GVL_OPC_test.";
        public int NamespaceId = 4;
        public int Test = 100;
    }
}
