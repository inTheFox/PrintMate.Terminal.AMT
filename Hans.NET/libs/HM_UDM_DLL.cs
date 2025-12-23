using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Hans.NET.libs
{
    public struct structUdmPos 
    {
	    public float x;
        public float y;
        public float z;
        public float a;
    };

    public struct MarkParameter
    {
        public uint MarkSpeed;//打标速度(mm/s)
        public uint JumpSpeed;//跳转速度(mm/s)
        public uint MarkDelay;//打标延时(us)
        public uint JumpDelay;//跳转延时(us)
        public uint PolygonDelay;//转弯延时(us)
        public uint MarkCount;//打标次数
        public float LaserOnDelay;//开激光延时(单位us)
        public float LaserOffDelay;//关激光延时(单位us)
        public float FPKDelay;//首脉冲抑制延时(单位us)
        public float FPKLength;//首脉冲抑制长度(单位us)
        public float QDelay;//出光Q频率延时(单位us)
        public float DutyCycle;//出光时占空比，(0~1)
        public float Frequency;//出光时频率kHz
        public float StandbyFrequency;//不出光Q频率(单位kHz);
        public float StandbyDutyCycle;//不出光Q占空比(0~1);
        public float LaserPower;//激光能量百分比(0~100)，50代表50%
        public uint AnalogMode;//1代表使用模拟量输出来控制激光器能量（0~10V）
        public uint Waveform;//SPI激光器波形号（0~63）
        public uint PulseWidthMode;//0,不开启MOPA脉宽使能模式， 1,开启MOPA激光器脉宽使能
        public uint PulseWidth;//MOPA激光器脉宽值 单位（ns）
    }

    public class HM_UDM_DLL
    {
        public const string DllName = "libs/HM_HashuScan.dll";

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_NewFile();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_SaveToFile(string strFilePath);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_GetUDMBuffer(ref nint pUdmBuffer, ref int nBytesCount);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_Main();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_EndMain();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_SetProtocol(int nProtocol, int nDimensional);//nProtocol:0表示SPI协议，1表示XY2-100协议，2表示SL2协议;nDimensional:0是2D打标，1是3D打标

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_RepeatStart(int repeatCount);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_RepeatEnd(int startAddress);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_Jump(float x, float y, float z);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_Wait(float msTime);//单位ms

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_SetGuidLaser(bool enable);//开启关闭红光

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_SetInput(uint uInIndex);//索引从0开始(0~7)，相应输入信号触发后才继续往下执行，否则一直等待输入信号。

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_SetOutPutAll(uint uData);//一键全部控制输出信号（此信息会写入UDM.BIN文件中去）"二进制1111代表四个输出全部拉高，二进制0011代表out0、out1拉高，out2/out3拉低"

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_SetOutPutOn(uint nOutIndex);//单独拉高输出信号（此信息会写入UDM.BIN文件中去）

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_SetOutPutOff(uint nOutIndex);//单独拉低输出信号（此信息会写入UDM.BIN文件中去）

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_SetOutputOn_GMC4(uint nOutIndex);//单独拉高输出信号（此信息会写入UDM.BIN文件中去）,GMC4卡

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_SetOutputOff_GMC4(uint nOutIndex);//单独拉低输出信号（此信息会写入UDM.BIN文件中去）,GMC4卡

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_SetAnalogValue(float VoutA, float VoutB);//设置两路(VOUTA/VOUTB)模拟量的值，0表示0V，0.5表示5V，1表示10V电压

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_SetOffset(float offsetX, float offsetY, float offsetZ);//所有点坐标平移

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_SetRotate(float angle, float centryX, float centryY);//旋转

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_FootTrigger(uint nDelayTime, int nTriggerType);//启用脚踏触发打标,nDelayTime触发后延时多久开始打标，单位ms。nTriggerType,0上升沿触发，1电平触发

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_SkyWriting(int enable);//0关闭SkyWriting， 1启用SkyWriting功能

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_SetSkyWritingMode(int enable, int mode, float uniformLen, float accLen, float angleLimit);//enable=0关闭SkyWriting， 1启用SkyWriting功能

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_SetJumpExtendLen(float jumpExtendLen);//设置跳转延长

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_SetLayersPara(MarkParameter[] layersParameter, int count);//layersParameter层数组，count层数组个数

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_AddPolyline2D(structUdmPos[] nPos, int nCount, int layerIndex);//nPos图形点数组，nCount点个数，layerIndex所在图层索引

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_AddPolyline3D(structUdmPos[] nPos, int nCount, int layerIndex);//nPos图形点数组，nCount点个数，layerIndex所在图层索引

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_AddPoint2D(structUdmPos pos, float time, int layerIndex);//pos打标点，time打此点时间ms，layerIndex所在图层索引

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_SetCloseLoop(bool enable, int galvoType, int followErrorMax, int followErrorCount);//设置闭环控制

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_Set3dCorrectionPara(float baseFocal, double[] paraK, int nCount);//设置3D校正表参数

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public extern static float UDM_GetZvalue(float x, float y, float height);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_AddBreakAndCorPolyline3D(structUdmPos[] nPos, int nCount, float p2pGap, int layerIndex);//nPos图形点数组，nCount点个数，layerIndex所在图层索引

    }


}
