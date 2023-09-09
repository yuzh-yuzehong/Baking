using System;

namespace Machine
{
    class DryingOvenClient : BaseThread
    {
        #region // 字段

        private FinsTCP finsTcp;            // fins客户端
        private byte[] sendBuffer;          // 发送缓存
        private byte[] recvBuffer;          // 接收缓存
        private DryingOvenData ovenData;    // 接收数据解析得到的干燥炉数据
        private int errCount;               // 接收数据错误次数计数
        private static DryOvenFinsCmd[] finsCmdAddr;    // 命令地址表

        #endregion

        #region // 命令地址表

        private static DryOvenFinsCmd[] FinsCmdAddr
        {
            get
            {
                if(null == finsCmdAddr)
                {
                    finsCmdAddr = new DryOvenFinsCmd[(int)DryOvenCmd.End]
                    {
                        //SenserState = 0,            // 传感器状态（读取）
                        new DryOvenFinsCmd(ZoneCode.DMWord, 3000, 10, 0, 0, 40),
                        //RunState,                   // 工作状态（读取）
                        new DryOvenFinsCmd(ZoneCode.DMWord, 3050, 10, 0, 0, 40),
                        //RunTemp,                    // 实时温度（读取）
                        new DryOvenFinsCmd(ZoneCode.DMWord, 3100, 160, 0, 0, 640),
                        //AlarmValue,                 // 报警值（温度、真空）（读取）
                        new DryOvenFinsCmd(ZoneCode.DMWord, 3900, 84, 0, 0, 336),
                        //AlarmState,                 // 报警状态（读取）
                        new DryOvenFinsCmd(ZoneCode.DMWord, 4320, 40, 0, 0, 160),
                        //GetParameter,               // 工艺参数（读）
                        new DryOvenFinsCmd(ZoneCode.DMWord, 4520, 50, 0, 0, 200),
                        //SystemInfo,                 // 干燥炉系统信息（读）
                        new DryOvenFinsCmd(ZoneCode.DMWord, 4800, 0, 0, 0, 20),

                        //SetParameter,               // 工艺参数（可读可写）
                        new DryOvenFinsCmd(ZoneCode.DMWord, 4520, 50, 0, 0, 20 * 2),
                        //WorkStartStop,              // 加热启动/停止（写入）
                        new DryOvenFinsCmd(ZoneCode.DMWord, 4720, 10, 0, 0, 1),
                        //DoorOpenClose,              // 炉门打开/关闭（写入）
                        new DryOvenFinsCmd(ZoneCode.DMWord, 4721, 10, 0, 0, 1),
                        //VacOpenClose,               // 真空打开/关闭（写入）
                        new DryOvenFinsCmd(ZoneCode.DMWord, 4722, 10, 0, 0, 1),
                        //BlowOpenClose,              // 破真空打开/关闭（写入）
                        new DryOvenFinsCmd(ZoneCode.DMWord, 4723, 10, 0, 0, 1),
                        //PressureOpenClose,          // 保压打开/关闭（写入）
                        new DryOvenFinsCmd(ZoneCode.DMWord, 4724, 10, 0, 0, 1),
                        //FaultReset,                 // 故障复位（写入）
                        new DryOvenFinsCmd(ZoneCode.DMWord, 4725, 10, 0, 0, 1),
                        //SetMcDoor,                  // 调度设备门禁（写入）
                        new DryOvenFinsCmd(ZoneCode.DMWord, 4801, 0, 0, 0, 1),
                    };
                }
                return finsCmdAddr;
            }
        }

        #endregion

        #region // 构造函数

        public DryingOvenClient()
        {
            this.finsTcp = new FinsTCP();
            this.ovenData = new DryingOvenData();
            this.sendBuffer = new byte[1024 * 5];
            this.recvBuffer = new byte[1024 * 5];
            this.errCount = 0;
        }

        #endregion

        #region // 编码解码

        /// <summary>
        /// 编解码模式
        /// </summary>
        enum CodecMode
        {
            // 16位字节顺序
            bit16_12 = 0,
            bit16_21,

            // 32位字节顺序
            bit32_1234,
            bit32_2143,
            bit32_3412,
            bit32_4321,
        };

        /// <summary>
        /// 字节序调整
        /// </summary>
        private void ByteCodec(byte[] data, int startIdx, int count, CodecMode codec)
        {
            if(null == data || count <= 0)
            {
                return;
            }
            switch(codec)
            {
                case CodecMode.bit16_12:
                    break;
                case CodecMode.bit16_21:
                    {
                        byte buf;
                        for(int idx = 0; idx < count; idx += 2)
                        {
                            buf = data[startIdx + idx];
                            data[startIdx + idx] = data[startIdx + idx + 1];
                            data[startIdx + idx + 1] = buf;
                        }
                        break;
                    }
                case CodecMode.bit32_1234:
                    break;
                case CodecMode.bit32_2143:
                    {
                        byte[] buf = new byte[4];
                        for(int idx = 0; idx < count; idx += 4)
                        {
                            Array.Copy(data, (startIdx + idx), buf, 0, 4);
                            data[startIdx + idx] = buf[1];
                            data[startIdx + idx + 1] = buf[0];
                            data[startIdx + idx + 2] = buf[3];
                            data[startIdx + idx + 3] = buf[2];
                        }
                        break;
                    }
                case CodecMode.bit32_3412:
                    {
                        byte[] buf = new byte[4];
                        for(int idx = 0; idx < count; idx += 4)
                        {
                            Array.Copy(data, (startIdx + idx), buf, 0, 4);
                            data[startIdx + idx] = buf[2];
                            data[startIdx + idx + 1] = buf[3];
                            data[startIdx + idx + 2] = buf[0];
                            data[startIdx + idx + 3] = buf[1];
                        }
                        break;
                    }
                case CodecMode.bit32_4321:
                    {
                        byte[] buf = new byte[4];
                        for(int idx = 0; idx < count; idx++)
                        {
                            Array.Copy(data, (startIdx + idx), buf, 0, 4);
                            data[startIdx + idx] = buf[3];
                            data[startIdx + idx + 1] = buf[2];
                            data[startIdx + idx + 2] = buf[1];
                            data[startIdx + idx + 3] = buf[0];
                        }
                        break;
                    }
            }
        }

        #endregion

        #region // 内部接口

        protected override void RunWhile()
        {
            if(IsConnect())
            {
                try
                {
                    DryingOvenData data = new DryingOvenData();
                    // 所有读取数据
                    for(int i = (int)DryOvenCmd.SenserState; i < (int)DryOvenCmd.SetParameter; i++)
                    {
                        recvBuffer.Initialize();
                        if(this.finsTcp.ReadWords(FinsCmdAddr[i].zone, FinsCmdAddr[i].wordAddr, FinsCmdAddr[i].bitAddr, FinsCmdAddr[i].count, ref recvBuffer))
                        {
                            BufToData((DryOvenCmd)i, FinsCmdAddr[i].count, FinsCmdAddr[i].wordInterval, ref data, recvBuffer);
                        }
                        else
                        {
                            data.DataError = true;
                            if(this.errCount < (2 * (int)DryOvenCmd.SetParameter))
                            {
                                this.errCount++;
                                WriteLog($"{this.finsTcp.GetIPInfo()}.DryingOvenClient.RunWhile() read {((DryOvenCmd)i).ToString()} fail.", HelperLibrary.LogType.Error);
                            }
                        }
                    }
                    this.ovenData.Copy(data);
                }
                catch(System.Exception ex)
                {
                    WriteLog("RunWhile() Exception: " + ex.Message, HelperLibrary.LogType.Error);
                }
            }
            Sleep(50);
        }

        private void BufToData(DryOvenCmd cmdID, int count, int dataInterval, ref DryingOvenData data, byte[] buffer)
        {
            switch(cmdID)
            {
                case DryOvenCmd.SenserState:
                    {
                        ByteCodec(buffer, 0, (count * (int)OvenRowCol.MaxRow), CodecMode.bit16_21);

                        for(int cavityIdx = 0; cavityIdx < (int)OvenRowCol.MaxRow; cavityIdx++)
                        {
                            int idx = (cavityIdx * dataInterval) * 2;
                            CavityData cavityData = data.CavityDatas[cavityIdx];

                            // 门状态
                            short value = BitConverter.ToInt16(buffer, idx);
                            if((value & 0xFF) == 0xC3)
                            {
                                cavityData.doorState = (short)OvenStatus.DoorOpen;
                            }
                            else if((value & 0xFF) == 0x3C)
                            {
                                cavityData.doorState = (short)OvenStatus.DoorClose;
                            }
                            else
                            {
                                cavityData.doorState = (short)OvenStatus.DoorAction;
                            }
                            // 安全光幕
                            cavityData.safetyCurtain = (short)(((value & 0x0100) == 0x0100) ? OvenStatus.SafetyCurtainOn : OvenStatus.SafetyCurtainOff);
                            // 夹具状态
                            value = BitConverter.ToInt16(buffer, (idx += 2));
                            for(int pltIdx = 0; pltIdx < (int)OvenRowCol.MaxCol; pltIdx++)
                            {
                                int tmp = (0x03 << (pltIdx * 2));
                                cavityData.palletState[pltIdx] = (short)(((value & tmp) > 0x00) ? OvenStatus.PalletHave : OvenStatus.PalletNot);
                            }
                            // 真空 破真空
                            value = BitConverter.ToInt16(buffer, (idx += 2));
                            for(int pltIdx = 0; pltIdx < (int)OvenRowCol.MaxCol; pltIdx++)
                            {
                                int tmp = (0x30 << (pltIdx * 2));
                                cavityData.heatingState[pltIdx] = (short)(((value & tmp) > 0x00) ? OvenStatus.HeatOpen : OvenStatus.HeatClose);
                            }
                            cavityData.vacValveState = (short)(((value & 0x40) > 0x00) ? OvenStatus.VacOpen : OvenStatus.VacClose);
                            cavityData.blowValveState = (short)(((value & 0x80) > 0x00) ? OvenStatus.BlowOpen : OvenStatus.BlowClose);
                        }
                        break;
                    }
                case DryOvenCmd.RunState:
                    {
                        ByteCodec(buffer, 0, (count * (int)OvenRowCol.MaxRow), CodecMode.bit32_2143);

                        for(int cavityIdx = 0; cavityIdx < (int)OvenRowCol.MaxRow; cavityIdx++)
                        {
                            int idx = (cavityIdx * dataInterval) * 2;
                            CavityData cavityData = data.CavityDatas[cavityIdx];

                            cavityData.workTime = BitConverter.ToUInt32(buffer, idx);
                            cavityData.workState = BitConverter.ToUInt32(buffer, (idx += 4));
                            cavityData.pressureState = BitConverter.ToUInt32(buffer, (idx += 4));
                            cavityData.vacPressure = BitConverter.ToUInt32(buffer, (idx += 4));
                        }
                        break;
                    }
                case DryOvenCmd.RunTemp:
                    {
                        ByteCodec(buffer, 0, (count * (int)OvenRowCol.MaxRow), CodecMode.bit32_2143);

                        for(int cavityIdx = 0; cavityIdx < (int)OvenRowCol.MaxRow; cavityIdx++)
                        {
                            int idx = (cavityIdx * dataInterval) * 2;
                            CavityData cavityData = data.CavityDatas[cavityIdx];

                            for(int pltIdx = 0; pltIdx < (int)OvenRowCol.MaxCol; pltIdx++)
                            {
                                for(int i = 0; i < 2; i++)
                                {
                                    for(int j = 0; j < (int)OvenInfoCount.HeatPanelCount; j++)
                                    {
                                        // 起始索引 + 夹具*(控温/巡检)*地址数量*每地址2字节 + 控温/巡检*地址数量*每地址2字节 + 发热板*4字节
                                        int valueIdx = idx + pltIdx * 2 * 40 * 2 + i * 40 * 2 + j * 4;
                                        cavityData.tempValue[pltIdx, i, j] = BitConverter.ToSingle(buffer, valueIdx);
                                    }
                                }
                            }
                        }
                        break;
                    }
                case DryOvenCmd.AlarmValue:
                    {
                        ByteCodec(buffer, 0, (count * (int)OvenRowCol.MaxRow), CodecMode.bit32_2143);

                        for(int cavityIdx = 0; cavityIdx < (int)OvenRowCol.MaxRow; cavityIdx++)
                        {
                            int idx = (cavityIdx * dataInterval) * 2;
                            CavityData cavityData = data.CavityDatas[cavityIdx];

                            cavityData.vacAlarmValue = Convert.ToDouble(BitConverter.ToUInt32(buffer, idx));
                            idx += 4 * 2;
                            for(int pltIdx = 0; pltIdx < (int)OvenRowCol.MaxCol; pltIdx++)
                            {
                                for(int j = 0; j < (int)OvenInfoCount.HeatPanelCount; j++)
                                {
                                    int valueIdx = idx + pltIdx * 2 * 40 + j * 4;
                                    cavityData.tempAlarmValue[pltIdx, j] = BitConverter.ToSingle(buffer, valueIdx);
                                }
                            }
                        }
                        break;
                    }
                case DryOvenCmd.AlarmState:
                    {
                        ByteCodec(buffer, 0, (count * (int)OvenRowCol.MaxRow), CodecMode.bit32_2143);

                        for(int cavityIdx = 0; cavityIdx < (int)OvenRowCol.MaxRow; cavityIdx++)
                        {
                            int idx = (cavityIdx * dataInterval) * 2;
                            CavityData cavityData = data.CavityDatas[cavityIdx];

                            cavityData.doorAlarm = (0 != BitConverter.ToInt16(buffer, (idx += 0)));
                            cavityData.vacAlarm = (0 != BitConverter.ToInt16(buffer, (idx += 2)));
                            cavityData.blowAlarm = (0 != BitConverter.ToInt16(buffer, (idx += 2)));
                            cavityData.vacuometerAlarm = (0 != BitConverter.ToInt16(buffer, (idx += 2)));
                            cavityData.faultAlarm = (0 != BitConverter.ToInt16(buffer, (idx += 2)));
                            for(int i = 0; i < cavityData.controlAlarm.Length; i++)
                            {
                                cavityData.controlAlarm[i] = (0 != BitConverter.ToInt16(buffer, (idx += 2)));
                            }
                            for(int i = 0; i < cavityData.pallletAlarm.Length; i++)
                            {
                                cavityData.pallletAlarm[i] = (0 != BitConverter.ToInt16(buffer, (idx += 2)));
                            }

                            idx = (cavityIdx * dataInterval) * 2 + 10 * 2;
                            for(int pltIdx = 0; pltIdx < (int)OvenRowCol.MaxCol; pltIdx++)
                            {
                                for(int j = 0; j < (int)OvenInfoCount.HeatPanelCount; j++)
                                {
                                    // 间隔其实地址 + 夹具 * 字间隔 * 每字2字节 + 发热板 / 16位
                                    int valueIdx = idx + pltIdx * 3 * 2;
                                    // 种类间隔
                                    int almInterval = 3 * 2;
                                    // 超温、低温、温差异常、超高温、信号异常
                                    bool[] state = new bool[(int)OvenTmpAlarm.End];
                                    short value = BitConverter.ToInt16(buffer, (valueIdx + j / 16 * 2));
                                    state[(int)OvenTmpAlarm.OverTmp] = (value & (0x01 << (j % 16))) > 0;
                                    value = BitConverter.ToInt16(buffer, (valueIdx + j / 16 * 2 + almInterval * 2));
                                    state[(int)OvenTmpAlarm.LowTmp] = (value & (0x01 << (j % 16))) > 0;
                                    value = BitConverter.ToInt16(buffer, (valueIdx + j / 16 * 2 + almInterval * 2 * 2));
                                    state[(int)OvenTmpAlarm.Difference] = (value & (0x01 << (j % 16))) > 0;
                                    value = BitConverter.ToInt16(buffer, (valueIdx + j / 16 * 2 + almInterval * 3 * 2));
                                    state[(int)OvenTmpAlarm.HighTmp] = (value & (0x01 << (j % 16))) > 0;
                                    value = BitConverter.ToInt16(buffer, (valueIdx + j / 16 * 2 + almInterval * 4 * 2));
                                    state[(int)OvenTmpAlarm.Exceptional] = (value & (0x01 << (j % 16))) > 0;

                                    int alarm = 0;
                                    for(int i = 0; i < (int)OvenTmpAlarm.End; i++)
                                    {
                                        if(state[i])
                                        {
                                            alarm |= (0x01 << i);
                                        }
                                    }
                                    cavityData.tempAlarm[pltIdx, j] = (short)alarm;
                                }
                            }
                        }
                        break;
                    }
                case DryOvenCmd.GetParameter:
                    {
                        ByteCodec(buffer, 0, (count * (int)OvenRowCol.MaxRow), CodecMode.bit32_2143);

                        for(int cavityIdx = 0; cavityIdx < (int)OvenRowCol.MaxRow; cavityIdx++)
                        {
                            int idx = (cavityIdx * dataInterval) * 2;
                            CavityData cavityData = data.CavityDatas[cavityIdx];

                            cavityData.parameter.SetTempValue = (float)(BitConverter.ToUInt32(buffer, (idx += 0)) / 100.0);
                            cavityData.parameter.TempUpperlimit = (float)(BitConverter.ToUInt32(buffer, (idx += 4)) / 100.0);
                            cavityData.parameter.TempLowerlimit = (float)(BitConverter.ToUInt32(buffer, (idx += 4)) / 100.0);
                            cavityData.parameter.PreheatTime = BitConverter.ToUInt32(buffer, (idx += 4));
                            cavityData.parameter.VacHeatTime = BitConverter.ToUInt32(buffer, (idx += 4));
                            cavityData.parameter.OpenDoorBlowTime = BitConverter.ToUInt32(buffer, (idx += 4));
                            cavityData.parameter.OpenDoorVacPressure = BitConverter.ToUInt32(buffer, (idx += 4));
                            cavityData.parameter.AStateVacTime = BitConverter.ToUInt32(buffer, (idx += 4));
                            cavityData.parameter.AStateVacPressure = BitConverter.ToUInt32(buffer, (idx += 4));
                            cavityData.parameter.BStateVacTime = BitConverter.ToUInt32(buffer, (idx += 4));
                            cavityData.parameter.BStateVacPressure = BitConverter.ToUInt32(buffer, (idx += 4));
                            cavityData.parameter.BStateBlowAirTime = BitConverter.ToUInt32(buffer, (idx += 4));
                            cavityData.parameter.BStateBlowAirPressure = BitConverter.ToUInt32(buffer, (idx += 4));
                            cavityData.parameter.BStateBlowAirKeepTime = BitConverter.ToUInt32(buffer, (idx += 4));
                            cavityData.parameter.BreathTimeInterval = BitConverter.ToUInt32(buffer, (idx += 4));
                            cavityData.parameter.BreathCycleTimes = BitConverter.ToUInt32(buffer, (idx += 4));
                            cavityData.parameter.HeatPlate = BitConverter.ToUInt32(buffer, (idx += 4));
                            cavityData.parameter.MaxNGHeatPlate = BitConverter.ToUInt32(buffer, (idx += 4));
                            cavityData.parameter.HeatPreVacTime = BitConverter.ToUInt32(buffer, (idx += 4));
                            cavityData.parameter.HeatPreBlow = BitConverter.ToUInt32(buffer, (idx += 4));
                        }
                        break;
                    }
                case DryOvenCmd.SystemInfo:
                    {
                        ByteCodec(buffer, 0, (count * (int)OvenRowCol.MaxRow), CodecMode.bit32_2143);

                        int idx = 0;
                        short value = BitConverter.ToInt16(buffer, idx);
                        data.RemoteState = (short)(((value & 0x01) == 0x01) ? OvenStatus.RemoteClose : OvenStatus.RemoteOpen);
                        value = BitConverter.ToInt16(buffer, (idx += 2));
                        data.MCDoorState = (short)(((value & 0x01) == 0x01) ? OvenStatus.McDoorClose : OvenStatus.McDoorOpen);
                    }
                    break;
                default:
                    break;
            }
        }

        private bool DataToBuf(DryOvenCmd cmdID, int count, CavityData cavityData, ref byte[] sendData)
        {
            bool result = false;
            switch(cmdID)
            {
                case DryOvenCmd.SetParameter:
                    {
                        result = true;
                        int idx = 0;
                        CavityParameter para = cavityData.parameter;
                        byte[] buf = BitConverter.GetBytes(Convert.ToUInt32(para.SetTempValue * 100));
                        Array.Copy(buf, 0, sendData, idx, buf.Length);
                        idx += buf.Length;
                        buf = BitConverter.GetBytes(Convert.ToUInt32(para.TempUpperlimit * 100));
                        Array.Copy(buf, 0, sendData, idx, buf.Length);
                        idx += buf.Length;
                        buf = BitConverter.GetBytes(Convert.ToUInt32(para.TempLowerlimit * 100));
                        Array.Copy(buf, 0, sendData, idx, buf.Length);
                        idx += buf.Length;
                        buf = BitConverter.GetBytes(para.PreheatTime);
                        Array.Copy(buf, 0, sendData, idx, buf.Length);
                        idx += buf.Length;
                        buf = BitConverter.GetBytes(para.VacHeatTime);
                        Array.Copy(buf, 0, sendData, idx, buf.Length);
                        idx += buf.Length;
                        buf = BitConverter.GetBytes(para.OpenDoorBlowTime);
                        Array.Copy(buf, 0, sendData, idx, buf.Length);
                        idx += buf.Length;
                        buf = BitConverter.GetBytes(para.OpenDoorVacPressure);
                        Array.Copy(buf, 0, sendData, idx, buf.Length);
                        idx += buf.Length;
                        buf = BitConverter.GetBytes(para.AStateVacTime);
                        Array.Copy(buf, 0, sendData, idx, buf.Length);
                        idx += buf.Length;
                        buf = BitConverter.GetBytes(para.AStateVacPressure);
                        Array.Copy(buf, 0, sendData, idx, buf.Length);
                        idx += buf.Length;
                        buf = BitConverter.GetBytes(para.BStateVacTime);
                        Array.Copy(buf, 0, sendData, idx, buf.Length);
                        idx += buf.Length;
                        buf = BitConverter.GetBytes(para.BStateVacPressure);
                        Array.Copy(buf, 0, sendData, idx, buf.Length);
                        idx += buf.Length;
                        buf = BitConverter.GetBytes(para.BStateBlowAirTime);
                        Array.Copy(buf, 0, sendData, idx, buf.Length);
                        idx += buf.Length;
                        buf = BitConverter.GetBytes(para.BStateBlowAirPressure);
                        Array.Copy(buf, 0, sendData, idx, buf.Length);
                        idx += buf.Length;
                        buf = BitConverter.GetBytes(para.BStateBlowAirKeepTime);
                        Array.Copy(buf, 0, sendData, idx, buf.Length);
                        idx += buf.Length;
                        buf = BitConverter.GetBytes(para.BreathTimeInterval);
                        Array.Copy(buf, 0, sendData, idx, buf.Length);
                        idx += buf.Length;
                        buf = BitConverter.GetBytes(para.BreathCycleTimes);
                        Array.Copy(buf, 0, sendData, idx, buf.Length);
                        idx += buf.Length;
                        buf = BitConverter.GetBytes(para.HeatPlate);
                        Array.Copy(buf, 0, sendData, idx, buf.Length);
                        idx += buf.Length;
                        buf = BitConverter.GetBytes(para.MaxNGHeatPlate);
                        Array.Copy(buf, 0, sendData, idx, buf.Length);
                        idx += buf.Length;
                        buf = BitConverter.GetBytes(para.HeatPreVacTime);
                        Array.Copy(buf, 0, sendData, idx, buf.Length);
                        idx += buf.Length;
                        buf = BitConverter.GetBytes(para.HeatPreBlow);
                        Array.Copy(buf, 0, sendData, idx, buf.Length);
                        idx += buf.Length;
                        ByteCodec(sendData, 0, count * 4, CodecMode.bit32_2143);    // 调整count * 4(每一组数据4字节)字节顺序
                        break;
                    }
                case DryOvenCmd.WorkStartStop:
                    {
                        result = true;
                        byte[] buf = BitConverter.GetBytes(cavityData.workState);
                        ByteCodec(buf, 0, count * 2, CodecMode.bit16_21);
                        Array.Copy(buf, 0, sendData, 0, buf.Length);
                        break;
                    }
                case DryOvenCmd.DoorOpenClose:
                    {
                        result = true;
                        byte[] buf = BitConverter.GetBytes(cavityData.doorState);
                        ByteCodec(buf, 0, count * 2, CodecMode.bit16_21);
                        Array.Copy(buf, 0, sendData, 0, buf.Length);
                        break;
                    }
                case DryOvenCmd.VacOpenClose:
                    {
                        result = true;
                        byte[] buf = BitConverter.GetBytes(cavityData.vacValveState);
                        ByteCodec(buf, 0, count * 2, CodecMode.bit16_21);
                        Array.Copy(buf, 0, sendData, 0, buf.Length);
                        break;
                    }
                case DryOvenCmd.BlowOpenClose:
                    {
                        result = true;
                        byte[] buf = BitConverter.GetBytes(cavityData.blowValveState);
                        ByteCodec(buf, 0, count * 2, CodecMode.bit16_21);
                        Array.Copy(buf, 0, sendData, 0, buf.Length);
                        break;
                    }
                case DryOvenCmd.PressureOpenClose:
                    {
                        result = true;
                        byte[] buf = BitConverter.GetBytes(cavityData.pressureState);
                        ByteCodec(buf, 0, count * 2, CodecMode.bit16_21);
                        Array.Copy(buf, 0, sendData, 0, buf.Length);
                        break;
                    }
                case DryOvenCmd.FaultReset:
                    {
                        result = true;
                        byte[] buf = BitConverter.GetBytes(cavityData.faultReset);
                        ByteCodec(buf, 0, count * 2, CodecMode.bit16_21);
                        Array.Copy(buf, 0, sendData, 0, buf.Length);
                        break;
                    }
                case DryOvenCmd.SetMcDoor:
                    {
                        result = true;
                        byte[] buf = BitConverter.GetBytes(cavityData.mcDoorState);
                        ByteCodec(buf, 0, count * 2, CodecMode.bit16_21);
                        Array.Copy(buf, 0, sendData, 0, buf.Length);
                        break;
                    }
                default:
                    break;
            }
            return result;
        }

        #endregion

        #region // 对外接口

        /// <summary>
        /// 连接状态
        /// </summary>
        /// <returns></returns>
        public bool IsConnect()
        {
            return this.finsTcp.IsConnect();
        }

        /// <summary>
        /// 连接
        /// </summary>
        /// <param name="ip">PLC的IP地址</param>
        /// <param name="port">PLC的端口号，默认9600</param>
        /// <param name="pcNodeID">PC节点号</param>
        /// <returns></returns>
        public bool Connect(string ip, int port, byte pcNodeID)
        {
            if(this.finsTcp.Connect(ip, port, pcNodeID))
            {
                InitThread(string.Format("DryingOvenClient {0}: {1} read Task", ip, port));
            }
            return IsConnect();
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        /// <returns></returns>
        public bool Disconnect()
        {
            bool result = this.finsTcp.Disconnect();
            ReleaseThread();
            this.ovenData.Release();
            this.errCount = 0;
            return result;
        }

        /// <summary>
        /// 写干燥炉数据
        /// </summary>
        /// <param name="cmdID"></param>
        /// <param name="cavityIdx"></param>
        /// <param name="cavityData"></param>
        /// <returns></returns>
        public bool SetDryOvenData(DryOvenCmd cmdID, int cavityIdx, CavityData cavityData)
        {
            DryOvenFinsCmd cmd = FinsCmdAddr[(int)cmdID];
            if(DataToBuf(cmdID, cmd.count, cavityData, ref sendBuffer))
            {
                return this.finsTcp.WriteWords(cmd.zone, (short)(cmd.wordAddr + cavityIdx * cmd.wordInterval), cmd.bitAddr, cmd.count, sendBuffer);
            }
            return false;
        }

        /// <summary>
        /// 获取干燥炉数据
        /// </summary>
        /// <param name="dryOvenData"></param>
        /// <returns></returns>
        public bool GetDryOvenData(ref DryingOvenData dryOvenData)
        {
            dryOvenData.Copy(this.ovenData);
            return true;
        }

        #endregion

    }
}
