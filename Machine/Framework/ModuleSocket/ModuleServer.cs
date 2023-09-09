using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;

namespace Machine
{
    sealed class ModuleServer : ServerSocket
    {
        #region // 字段

        private Dictionary<Socket, object> clientLock;
        private Dictionary<Socket, byte[]> recvBuffer;
        private Dictionary<Socket, int> recvBufVaildLen;
        private ModuleSocketData writeData;

        #endregion


        #region // 方法

        public ModuleServer()
        {
            this.clientLock = new Dictionary<Socket, object>();
            this.recvBuffer = new Dictionary<Socket, byte[]>();
            this.recvBufVaildLen = new Dictionary<Socket, int>();
            this.writeData = new ModuleSocketData();
            this.writeData.CreateData();
        }

        public void AddServerData(RunID runId, bool isDryOven = false)
        {
            lock(this.writeData.dataLock)
            {
                // 模组数据
                this.writeData.moduleEnable.Add(runId, false);
                this.writeData.moduleRunning.Add(runId, false);
                this.writeData.deviceIsConnect.Add(runId, false);
                this.writeData.moduleEvent.Add(runId, null);
                this.writeData.pallet.Add(runId, null);
                this.writeData.battery.Add(runId, null);
                this.writeData.batteryLine.Add(runId, null);
                this.writeData.pltPosSenser.Add(runId, null);
                this.writeData.pltPosEnable.Add(runId, null);
                this.writeData.robotAction.Add(runId, null);
                this.writeData.robotRunning.Add(runId, true);
                if(isDryOven)
                {
                    this.writeData.cavityState.Add(runId, null);
                    this.writeData.cavityTime.Add(runId, null);
                    this.writeData.cavityEnable.Add(runId, null);
                    this.writeData.cavityPressure.Add(runId, null);
                    this.writeData.cavityTransfer.Add(runId, null);
                    this.writeData.cavitySamplingCycle.Add(runId, null);
                    this.writeData.cavityHeartCycle.Add(runId, null);
                    this.writeData.waterContentValue.Add(runId, null);
                }
            }
        }

        /// <summary>
        /// 接收数据及回复
        /// </summary>
        /// <param name="sock"></param>
        protected override void Recv(Socket sock)
        {
            try
            {
                byte[] buffer = new byte[(int)MCSBuffer.Recv];
                int recvSize = sock.Receive(buffer);
                if(recvSize > 0)
                {
                    if(!this.clientLock.ContainsKey(sock))
                    {
                        this.clientLock.Add(sock, new object());
                    }
                    if(!this.recvBuffer.ContainsKey(sock))
                    {
                        this.recvBuffer.Add(sock, new byte[(int)MCSBuffer.Recv]);
                    }
                    if(!this.recvBufVaildLen.ContainsKey(sock))
                    {
                        this.recvBufVaildLen.Add(sock, 0);
                    }
                    PackageJoint(sock, buffer, recvSize);
                }
            }
            catch (System.Exception ex)
            {
                this.clientLock.Remove(sock);
                this.recvBuffer.Remove(sock);
                this.recvBufVaildLen.Remove(sock);
                WriteLog($"Recv() error. remove {sock.RemoteEndPoint.ToString()}");
                WriteLog($"Recv() error {ex.Message}");
            }
        }

        /// <summary>
        /// 拼包
        /// </summary>
        /// <param name="recvBuf"></param>
        /// <param name="size"></param>
        private void PackageJoint(Socket sock, byte[] recvBuf, int size)
        {
            int packHead = FindPackageHead(recvBuf, 0, size);
            int packEnd = FindPackageHead(recvBuf, packHead + 1, size);

            // 起始位有报头，且数据包有两个报头：完整报文 + 后报文开始
            if((packHead == 0) && (packEnd > 0))
            {
                // 第一个报文
                Array.Copy(recvBuf, 0, this.recvBuffer[sock], 0, (packEnd - packHead));
                AnalysisPackage(sock, this.recvBuffer[sock], (packEnd - packHead));
                this.recvBufVaildLen[sock] = 0;
                // 第二个报文
                Array.Copy(recvBuf, packEnd, recvBuf, 0, (size - packEnd));
                recvBuf[size - packEnd + 1] = Convert.ToByte("\0");
                PackageJoint(sock, this.recvBuffer[sock], (size - packEnd));
            }
            // 起始位仅有一个报头
            else if((packHead == 0) && (packEnd < 0))
            {
                // 仅有一个完整报文
                Array.Copy(recvBuf, 0, this.recvBuffer[sock], 0, size);
                if(AnalysisPackage(sock, this.recvBuffer[sock], size))
                {
                    this.recvBufVaildLen[sock] = 0;
                }
                // 报文不完整
                else
                {
                    this.recvBufVaildLen[sock] += size;
                }
            }
            // 前报文后续 + 后报文开始
            else if(packHead > 0)
            {
                // 前报文后续
                Array.Copy(recvBuf, 0, this.recvBuffer[sock], this.recvBufVaildLen[sock], packHead);
                AnalysisPackage(sock, this.recvBuffer[sock], (this.recvBufVaildLen[sock] + packHead));
                this.recvBufVaildLen[sock] = 0;
                // 后报文开始
                Array.Copy(recvBuf, packHead, recvBuf, 0, (size - packHead));
                PackageJoint(sock, recvBuf, (size - packHead));
            }
            // 无报头
            else if(packHead < 0)
            {
                // 前报文后续
                Array.Copy(recvBuf, 0, this.recvBuffer[sock], this.recvBufVaildLen[sock], packHead);
                if(AnalysisPackage(sock, this.recvBuffer[sock], (this.recvBufVaildLen[sock] + packHead)))
                {
                    this.recvBufVaildLen[sock] = 0;
                }
                // 报文不完整
                else
                {
                    this.recvBufVaildLen[sock] += size;
                }
            }
        }

        /// <summary>
        /// 查找报文中是否含有报头
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="startIdx"></param>
        /// <returns></returns>
        private int FindPackageHead(byte[] buf, int startIdx, int bufLen)
        {
            int index = -1;
            for(int i = startIdx; i < bufLen - PacketHeader.header.Length; i++)
            {
                bool find = true;
                byte[] check = new byte[PacketHeader.header.Length];
                Array.Copy(buf, i, check, 0, check.Length);
                for(int j = 0; j < check.Length; j++)
                {
                    if (check[j] != PacketHeader.header[j])
                    {
                        find = false;
                        break;
                    }
                }
                if (find)
                {
                    index = i;
                    break;
                }
            }
            return index;
        }

        /// <summary>
        /// 解析报文至ModuleSocketData
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        private bool AnalysisPackage(Socket sock, byte[] buf, int size)
        {
            PacketHeader head = new PacketHeader();
            int idx = PacketHeader.header.Length;
            head.cmdType = BitConverter.ToUInt32(buf, idx);
            idx += 4;
            head.length = BitConverter.ToUInt32(buf, idx);
            idx += 4;
            head.crcCode = BitConverter.ToUInt32(buf, idx);
            idx += 4;
            if ((uint)PacketType.ReadAll == head.cmdType)
            {
                if(GetModuleData(head.cmdType, ref this.writeData))
                {
                    return SendAndWait(sock, head.cmdType, this.writeData);
                }
            }
            else if(head.length == (uint)(size - idx))
            {
                byte[] data = new byte[head.length];
                Array.Copy(buf, idx, data, 0, head.length);
                uint crcCheck = (uint)Def.CRCCalc(data, (int)head.length);
                data = GZipCompress.Decompress(data);
                // 校验数据
                if(crcCheck == head.crcCode)
                {
                    ModuleSocketData socketData = ModuleSocketData.Deserialize(data, data.Length) as ModuleSocketData;
                    if(null != socketData)
                    {
                        if(SetModuleData(head.cmdType, socketData))
                        {
                            return SendAndWait(sock, head.cmdType, socketData);
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 获取发送数据报文
        /// </summary>
        /// <param name="dataType"></param>
        /// <param name="sourceData"></param>
        /// <param name="sendBuf"></param>
        /// <param name="sendSize"></param>
        /// <returns></returns>
        private bool GetSendData(uint dataType, ModuleSocketData sourceData, ref byte[] sendBuf, ref int sendSize)
        {
            try
            {
                byte[] buf = new byte[(int)MCSBuffer.Send];
                int size = 0;
                if(null != sourceData)
                {
                    size = sourceData.Serialize(ref buf);
                    buf = GZipCompress.Compress(buf, size);
                    size = buf.Length;
                }
                PacketHeader head = new PacketHeader();
                head.cmdType = dataType;
                head.crcCode = (uint)Def.CRCCalc(buf, size);

                // 拷贝数据
                int idx = PacketHeader.header.Length;
                PacketHeader.header.CopyTo(sendBuf, 0);
                BitConverter.GetBytes(head.cmdType).CopyTo(sendBuf, idx);
                idx += 4;
                BitConverter.GetBytes(size).CopyTo(sendBuf, idx);
                idx += 4;
                BitConverter.GetBytes(head.crcCode).CopyTo(sendBuf, idx);
                idx += 4;
                Array.Copy(buf, 0, sendBuf, idx, size);
                // 发送数据总大小
                sendSize = size + idx;

                return true;
            }
            catch(System.Exception ex)
            {
                WriteLog("ModuleServer GetSendData: " + ex.Message);
            }
            return false;
        }

        /// <summary>
        /// 发送并等待一次接收
        /// </summary>
        /// <param name="dataType"></param>
        /// <param name="writeData"></param>
        /// <returns></returns>
        private bool SendAndWait(Socket sock, uint dataType, ModuleSocketData writeSocket)
        {
            lock(this.clientLock[sock])
            {
                int sendSize = 0;
                byte[] sendBuf = new byte[(int)MCSBuffer.Send];
                if(GetSendData(dataType, writeData, ref sendBuf, ref sendSize))
                {
                    if(sock.Send(sendBuf, sendSize, SocketFlags.None) == sendSize)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 获取模组数据
        /// </summary>
        /// <param name="dataType"></param>
        /// <param name="socketData"></param>
        /// <returns></returns>
        private bool GetModuleData(uint dataType, ref ModuleSocketData socketData)
        {
            lock(socketData.dataLock)
            {
                MachineCtrl mc = MachineCtrl.GetInstance();

                socketData.machineID = (int)mc.MachineID;
                socketData.machineState = (int)mc.RunsCtrl.GetMCState();
                for(int i = 0; i < socketData.safeDoor.Length; i++)
                {
                    socketData.safeDoor[i] = mc.SafeDoorIsOpen(i);
                }
                RunID[] idList = socketData.moduleEnable.Keys.ToArray();
                for(int i = 0; i < idList.Length; i++)
                {
                    RunProcess run = mc.GetModule(idList[i]);
                    socketData.moduleEnable[idList[i]] = run.IsModuleEnable();
                    socketData.moduleRunning[idList[i]] = run.IsRunning();
                    //if((((uint)PackDataType.TypePallet & dataType) > 0) && (null != socketData.pallet))
                    {
                        socketData.pallet[idList[i]] = run.Pallet;
                        if((null == socketData.pltPosSenser[idList[i]]) || (socketData.pltPosSenser[idList[i]].Length != run.Pallet.Length))
                        {
                            socketData.pltPosSenser[idList[i]] = new int[run.Pallet.Length];
                        }
                        for(int j = 0; j < run.Pallet.Length; j++)
                        {
                            bool hasPlt = run.PalletKeepFlat(j, true, false);
                            bool noPlt = run.PalletKeepFlat(j, false, false);
                            // 无夹具
                            if(!hasPlt && noPlt)
                            {
                                socketData.pltPosSenser[idList[i]][j] = (int)OvenStatus.PalletNot;
                            }
                            // 有夹具
                            else if(hasPlt && !noPlt)
                            {
                                socketData.pltPosSenser[idList[i]][j] = (int)OvenStatus.PalletHave;
                            }
                            // 错误
                            else
                            {
                                socketData.pltPosSenser[idList[i]][j] = (int)OvenStatus.PalletErrror;
                            }
                        }
                    }
                    //if(((uint)PackDataType.TypeEvent & dataType) > 0)
                    {
                        if((null != run.moduleEvent) && (run.moduleEvent.Count > 0))
                        {
                            if(socketData.moduleEvent.ContainsKey(idList[i]))
                            {
                                socketData.moduleEvent[idList[i]] = run.moduleEvent.Values.ToArray();
                            }
                            else
                            {
                                socketData.moduleEvent.Add(idList[i], run.moduleEvent.Values.ToArray());
                            }
                        }
                    }
                    switch(idList[i])
                    {
                        case RunID.OnloadRobot:
                            {
                                RunProcessOnloadRobot rbt = run as RunProcessOnloadRobot;
                                if (null != rbt)
                                {
                                    socketData.pltPosEnable[idList[i]] = rbt.PalletPosEnable;
                                    if(null == socketData.robotAction[idList[i]])
                                    {
                                        socketData.robotAction[idList[i]] = new RobotActionInfo[2];
                                    }
                                    socketData.robotAction[idList[i]][0] = rbt.GetRobotActionInfo(true);
                                    socketData.robotAction[idList[i]][1] = rbt.GetRobotActionInfo(false);
                                    socketData.deviceIsConnect[idList[i]] = rbt.RobotIsConnect();
                                    socketData.robotRunning[idList[i]] = rbt.RobotRunning;
                                }
                                socketData.onloadCount = TotalData.OnloadCount;
                                socketData.onScanNGCount = TotalData.OnScanNGCount;
                                // 工艺参数
                                socketData.mesBillNo = MesResources.BillNo;
                                socketData.mesBillNum = MesResources.BillNum;
                                MesConfig cfg = MesDefine.GetMesCfg(MesInterface.ApplyTechProParam);
                                if(socketData.mesBillParamDate != cfg.parameterDate)
                                {
                                    MesConfig cfgTmp = new MesConfig();
                                    cfgTmp.Copy(cfg);
                                    socketData.mesBillParamDate = cfgTmp.parameterDate;
                                    socketData.mesBillParam.Clear();
                                    // 0407 注释
                                    //socketData.mesBillParam = cfgTmp.parameter;
                                }
                                break;
                            }
                        case RunID.Transfer:
                            {
                                RunProcessRobotTransfer rbt = run as RunProcessRobotTransfer;
                                if(null != rbt)
                                {
                                    if(null == socketData.robotAction[idList[i]])
                                    {
                                        socketData.robotAction[idList[i]] = new RobotActionInfo[2];
                                    }
                                    socketData.robotAction[idList[i]][0] = rbt.GetRobotActionInfo(true);
                                    socketData.robotAction[idList[i]][1] = rbt.GetRobotActionInfo(false);
                                    socketData.robotRunning[idList[i]] = rbt.RobotRunning;
                                    socketData.deviceIsConnect[idList[i]] = rbt.RobotIsConnect();
                                }
                                break;
                            }
                        case RunID.PalletBuffer:
                            {
                                RunProcessPalletBuffer pltBuf = run as RunProcessPalletBuffer;
                                if(null != pltBuf)
                                {
                                    socketData.pltPosEnable[idList[i]] = pltBuf.BufferEnable;
                                }
                                break;
                            }
                        case RunID.OffloadBattery:
                            {
                                RunProcessOffloadBattery rbt = run as RunProcessOffloadBattery;
                                if(null != rbt)
                                {
                                    if(null == socketData.robotAction[idList[i]])
                                    {
                                        socketData.robotAction[idList[i]] = new RobotActionInfo[2];
                                    }
                                    socketData.robotAction[idList[i]][0] = rbt.GetRobotActionInfo(true);
                                    socketData.robotAction[idList[i]][1] = rbt.GetRobotActionInfo(false);
                                }
                                socketData.offloadCount = TotalData.OffloadCount;
                                socketData.bakedNGCount = TotalData.BakedNGCount;
                                break;
                            }
                        case RunID.CoolingSystem:
                            {
                                RunProcessCoolingSystem rbt = run as RunProcessCoolingSystem;
                                if(null != rbt)
                                {
                                    socketData.batteryLine[idList[i]] = rbt.BatteryLine;
                                }
                                break;
                            }
                        default:
                            {
                                if((idList[i] >= RunID.DryOven0) && (idList[i] < RunID.DryOvenALL))
                                {
                                    RunProcessDryingOven oven = run as RunProcessDryingOven;
                                    if(null != oven)
                                    {
                                        socketData.deviceIsConnect[idList[i]] = oven.DryOvenIsConnect();
                                        socketData.cavityEnable[idList[i]] = oven.CavityEnable;
                                        socketData.cavityPressure[idList[i]] = oven.CavityPressure;
                                        socketData.cavityTransfer[idList[i]] = oven.CavityTransfer;
                                        socketData.cavitySamplingCycle[idList[i]] = oven.CavitySamplingCycle;
                                        socketData.cavityHeartCycle[idList[i]] = oven.CavityHeartCycle;

                                        if(null == socketData.cavityState[idList[i]])
                                            socketData.cavityState[idList[i]] = new int[(int)OvenRowCol.MaxRow];
                                        if(null == socketData.cavityTime[idList[i]])
                                            socketData.cavityTime[idList[i]] = new uint[(int)OvenRowCol.MaxRow];
                                        for(int j = 0; j < oven.CavityState.Length; j++)
                                        {
                                            socketData.cavityState[idList[i]][j] = (int)oven.CavityState[j];
                                            socketData.cavityTime[idList[i]][j] = oven.RCavity(j).workTime;
                                        }
                                    }
                                }
                                break;
                            }
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// 设置通讯数据至模组数据
        /// </summary>
        /// <param name="dataType"></param>
        /// <param name="socketData"></param>
        /// <returns></returns>
        private bool SetModuleData(uint dataType, ModuleSocketData socketData)
        {
            if(((uint)PacketType.SetPallet & dataType) > 0)
            {
                RunID[] runId = socketData.pallet.Keys.ToArray();
                for(int i = 0; i < runId.Length; i++)
                {
                    RunProcess run = MachineCtrl.GetInstance().GetModule(runId[i]);
                    if((null != run.Pallet) && (run.Pallet.Length > 0))
                    {
                        // 仅在信号为Start状态即在取放过程中下允许写夹具数据
                        for(int idx = 0; idx < socketData.pallet[runId[i]].Length; idx++)
                        {
                            if (null != socketData.pallet[runId[i]][idx])
                            {
                                run.Pallet[idx].Copy(socketData.pallet[runId[i]][idx]);
                                run.SaveRunData(SaveType.Pallet, idx);
                            }
                        }
                        return true;
                    }
                }
            }
            if(((uint)PacketType.SetEvent & dataType) > 0)
            {
                RunID[] runId = socketData.moduleEvent.Keys.ToArray();
                for(int i = 0; i < runId.Length; i++)
                {
                    if((null != socketData.moduleEvent) && (socketData.moduleEvent.Count > 0))
                    {
                        RunProcess run = MachineCtrl.GetInstance().GetModule(runId[i]);
                        if (null != run)
                        {
                            for(int idx = 0; idx < socketData.moduleEvent.Values.Count; idx++)
                            {
                                ModuleEvent evt = socketData.moduleEvent[runId[i]][idx];
                                run.SetEvent(run, evt.Event, evt.State, evt.Pos);
                            }
                            return true;
                        }
                    }
                }
            }
            if(((uint)PacketType.SetWaterContent & dataType) > 0)
            {
                RunID[] runId = socketData.waterContentValue.Keys.ToArray();
                for(int i = 0; i < runId.Length; i++)
                {
                    if((null != socketData.waterContentValue) && (socketData.waterContentValue.Count > 0))
                    {
                        RunProcessDryingOven run = MachineCtrl.GetInstance().GetModule(runId[i]) as RunProcessDryingOven;
                        if (null != run)
                        {
                            for(int row = 0; row < socketData.waterContentValue[runId[i]].GetLength(0); row++)
                            {
                                bool result = true;
                                double[] water = new double[3];
                                for(int idx = 0; idx < water.Length; idx++)
                                {
                                    water[idx] = socketData.waterContentValue[runId[i]][row, idx];
                                    if (water[idx] <= 0.0)
                                    {
                                        result = false;
                                    }
                                }
                                if (result)
                                {
                                    run.SetWaterContent(row, water);
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }
        
        #endregion

    }
}
