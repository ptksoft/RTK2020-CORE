using System;
using System.Collections.Generic;
using System.Text;

namespace MAIN
{
	#region LANE_TYPE
	public enum LANE_TYPE
	{
		OPEN = 0,
		ENTRY = 1,
		EXIT = 2,
	}
	#endregion
	#region LANE_ROLE
	public enum LANE_ROLE {
		MASTER = 0,
		SLAVE = 1,
		ALONE = -1,
	}
	#endregion
	#region LANE_MODE
	public enum LANE_MODE
	{
		CONFIG = -1,		// Error in CONFIG
		UNKNOWN = 0,		// Unknow Mode

		OPEN = 1,
		HPMC = 2,
		CLOSED = 3,
		TEST = 4,			// For Testing Equipment ONLY
		DEMO = 5,			// For Demo Queue Sequence
        
		DUAL = 11,			// Working with MTC
        SLFF = 21,          // Testing for KPS 2019-09-11
		FREEFLOW = 22,		// Reserve for future
		
        SLEEP = 88,         // Ignore All Sensor
        
		READ1 = 90,			// Read One OBU
		TOPUP = 91,			// Topup Money to OBU
		SIM = 99			// For Simulation Transaction Like OPEN Mode
	}
	#endregion
	#region LANE_THREAD
	public enum LANE_THREAD
	{
		ALB,			// Process ALB State
		CONSOLE,		// Process Command-Line
		EVENT,			// Event Raise from Other Module
		EQS,			// Process Equipment State
		LOGIC,			// Process LOGIC Cycle
		C_OBU,			// Check OBU Version
		C_TARIFF,		// Check Tariff Version
		ANT1_PKG,		// Process Antenna1 Packet IN
		ANT1_MON,		// Monitoring Antenna1 Connection
		ANT2_PKG,		// Process Antenna2 Packet IN
		ANT2_MON,		// Monitoring Antenna2 Connection
		AVC_PKG,		// Process AVC Packet IN
		AVC_MON,		// Monitoring AVC Connection
		CAM1_SNAP,		// Snap picture from camera
		CAM1_UPLD,		// Upload picture file to Storage
		CAM1_MON,		// Monitoring Camera Connection
		CRS_SEND,		// Send CRS command to Device
		CRS_MON,		// Monitoring CRS Connection
		FRONT,			// Front Sending
		IO_SIREN,		// Siren ALARM On/Off
		PLAZA,			// Plaza Update every 15 Sec
		TFI_SEND,		// Send TFI command to Device
		TFI_MON,		// Monitoring TFI Conneciton
        MFI_SEND,       // Send MFI command to Device
        MFI_MON,        // Monitoring MFI Connection
		
	}
	#endregion
		
	#region EFC_MMI
	public enum EFC_MMI
	{
		OK = 0,
		ERROR = 1,
		WARNING = 2,
		SILENT = 0xFF       // No Beep
	}
	#endregion
	
	#region EQL - Equipment List
	public enum EQL
	{
		OTL = 0,
		MLB,
		OB1, LOOP1,
		ANT1, ANT2,
		LTL, TFI, MFI,
		ALB, BELL, SIREN,
		OB2, LOOP2,
		AXEL1, AXEL2,
		DUALTIRE1, DUALTIRE2, DUALTIRE3,
		AVC,
		CAMERA,
		CRS, BALARM,
		PLAZA,
		ACTL,
		MASTER,
		QTOUCH,
		MTC,
	}
	#endregion	
	#region EQS - Equipment State
	public enum EQS			// EQuipment State
	{
		ERROR = -9,
		OFFLINE = -2,
		NA = -1,
		OFF = 0,		
		ON = 1,
		ONLINE = 2,
	}
	#endregion	
	#region EVL - Event List
	public enum EVL {
		ObuRead,
		ObuTouch,
		ObuFail,
		CarEntry,
		CarBack,
		CarExit,
		AVC,		
	}
	#endregion
	
	#region PASSAGE_CODE
	public enum PASSAGE_CODES
	{
		C00_ReconciliationRecord = 0,

        C02_ApprovedPassInEtcLane = 2,          //* cut_money - Normal Case
		C05_HistoricalPassNetworkFail = 5,      //  cut_money
		C08_UndetectedVehiclePass = 8,
		C09_AdditionalObuInVehicle = 9,
		C10_ApprovedPassButLowBalance = 10,     //  cut_money
		C19_ApprovedPassButMultiOBU = 19,       //  cut_money

		C20_PassWithNoEnoughtMoney_IMG = 20,
		C22_PassWithNoOBU_IMG = 22,
		C23_PassWithUnknowOBU_IMG = 23,
		C24_PassWithWriteFail_IMG = 24,
		C26_PassWithBlackList_IMG = 26,
		C41_PassWhileLaneClosed = 41,
		C42_PassWhileLaneMaintenance = 42,
        C43_PassWhileLaneHPMC = 43,
		C80_NewLaneMode = 80,
		C81_OverrideALB = 81,
		C82_PassByManualRead = 82,              // cut_money
		C90_PassReverseBackOut = 90,
		C93_PassWithUnknowEntry = 93,			// Unknow Entry

		C99_PassWithUnknowCondition_IMG = 99
	}
	#endregion	
	#region SIGNAL_CODE
	public enum SIGNAL_CODE
	{
		B1_GreenLight_ApprovedPassage = 0,      //Approved passage
		B2_GreenLight_LowBalance,
		B3_RedLight_InvalidPassage,
		B4_ObuNotDetected,
		B5_ObuNotDefinedInStatusList,
		B6_ObuMatchToSameVehicle,
		B7_ObuAuthorizationError,
		B9_InsufficientPayment,
		B10_UnconfirmedObuRead,
		B11_UnconfirmedObuUpdate,
		B12_InvalidEntryData,               // No tariff information available
		B13_ObuBlackList,
		B14_HistoricalPassage,              // Due to network fail
		B15_,
		B16_VideoPictureTakenOfThePassage,

	}
	#endregion	
}
