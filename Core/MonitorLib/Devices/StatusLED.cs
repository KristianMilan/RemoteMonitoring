using System;
using Raspberry.IO;
using Raspberry.IO.GeneralPurpose;

namespace Monitoring
{
	public static class StatusLED
	{
		private static OutputPinConfiguration RED_PIN;// = ConnectorPin.P1Pin12.Output();
		private static OutputPinConfiguration GREEN_PIN;// = ConnectorPin.P1Pin16.Output();
		private static OutputPinConfiguration BLUE_PIN;// = ConnectorPin.P1Pin21.Output();

		private static OutputPinConfiguration[] pins;// = new OutputPinConfiguration[3];

		private static Raspberry.IO.GeneralPurpose.GpioConnection cnx;

		static StatusLED ()
		{
			try
			{	RED_PIN = ConnectorPin.P1Pin12.Output();
				GREEN_PIN = ConnectorPin.P1Pin16.Output();
				BLUE_PIN = ConnectorPin.P1Pin21.Output();

				pins = new OutputPinConfiguration[3];
				pins [0] = RED_PIN;
				pins [1] = GREEN_PIN;
				pins [2] = BLUE_PIN;
				cnx = new GpioConnection(pins);
				cnx.Open ();
			}
			catch {
			}
		}

		public static Tuple<bool,bool,bool> GetColor()
		{
			try
			{
				return new Tuple<bool,bool,bool> (StatusHasRed, StatusHasGreen, StatusHasBlue);
			}
			catch {
				return new Tuple<bool, bool, bool>(false, false, false);
			}
		}

		public static void SetColor(Tuple<bool,bool,bool> rgb)
		{
			if (cnx != null && cnx.IsOpened)
			SetColor (rgb.Item1, rgb.Item2, rgb.Item3);
		}
		public static void SetColor(bool red, bool green, bool blue)
		{
			if (cnx != null && cnx.IsOpened) {
				RED_PIN.Enabled = red;
				GREEN_PIN.Enabled = green;
				BLUE_PIN.Enabled = blue;

				cnx.Toggle (RED_PIN);
				cnx.Toggle (GREEN_PIN);
				cnx.Toggle (BLUE_PIN);
			}
		}

		public static bool StatusHasRed{get{
				try
				{
					return RED_PIN.Enabled;
				}
				catch {
				}
				return false;
				}}
		public static bool StatusHasGreen{get{
				try
				{return GREEN_PIN.Enabled;
			}
				catch {
					return false;
			}}}
		public static bool StatusHasBlue{get{
				try
				{return BLUE_PIN.Enabled;
			}
				catch {
					return false;
			}}}
	}
}

