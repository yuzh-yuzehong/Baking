using HelperLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static SystemControlLibrary.DataBaseRecord;

namespace Machine
{
    class RunProTest : RunProcess
    {
        public RunProTest(int RunID) : base(RunID)
        {
            PowerUpRestart();

            this.inputMap.Add("X0001", X0001);
            this.inputMap.Add("X0002", X0002);
            this.inputMap.Add("X0003", X0003);
            this.inputMap.Add("X0004", X0004);
            this.inputMap.Add("X0005", X0005);

            this.outputMap.Add("Y0001", Y0001);
            this.outputMap.Add("Y0002", Y0002);
            this.outputMap.Add("Y0003", Y0003);
            this.outputMap.Add("Y0004", Y0004);
            this.outputMap.Add("Y0005", Y0005);
            this.outputMap.Add("Y0006", Y0006);

            this.motorMap.Add("M0001", M0001);
            this.motorMap.Add("M0002", M0002);
            this.motorMap.Add("M0003", M0003);
            this.motorMap.Add("M0004", M0004);
            this.motorMap.Add("M0005", M0005);
            this.motorMap.Add("M0006", M0006);
            this.motorMap.Add("M0007", M0007);

            InsertGroupParameter("int01", "参数int01", "必须为int", int01, RecordType.RECORD_INT);
            InsertVoidParameter("bool02", "参数bool02", "必须为bool", bool02, RecordType.RECORD_BOOL);
            InsertVoidParameter("double03", "参数double03", "必须为浮点数double", double03, RecordType.RECORD_DOUBLE);
            InsertVoidParameter("string04", "参数string04", "必须为字符串string", string04, RecordType.RECORD_STRING);
        }

        #region // 属性

        // inputs
        int X0001 = -1;
        int X0002 = -1;
        int X0003 = -1;
        int X0004 = -1;
        int X0005 = -1;

        // outputs
        int Y0001 = -1;
        int Y0002 = -1;
        int Y0003 = -1;
        int Y0004 = -1;
        int Y0005 = -1;
        int Y0006 = -1;

        // motors
        int M0001 = -1;
        int M0002 = -1;
        int M0003 = -1;
        int M0004 = -1;
        int M0005 = -1;
        int M0006 = -1;
        int M0007 = -1;

        // parameters
        int int01 = -1;
        bool bool02 = false;
        double double03 = 0.0;
        string string04 = "";

        #endregion

        protected override void AutoOperation()
        {
            if(!IsModuleEnable())
            {
                CurMsgStr("模组禁用", "Moudle not enable");
                Sleep(100);
                return;
            }

            if (Def.IsNoHardware())
            {
                Sleep(10);
            }

            switch((AutoSteps)this.nextAutoStep)
            {
                case AutoSteps.Auto_WaitWorkStart:
                    {
                        CurMsgStr("等待开始信号", "Wait work start");
                        this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                        break;
                    }
                case AutoSteps.Auto_WorkEnd:
                    {
                        CurMsgStr("工作完成", "Work end");
                        break;
                    }
                default:
                    {
                        Trace.Assert(false, "RunEx::AutoOperation/no this run step");
                        break;
                    }
            }
        }
    }
}
