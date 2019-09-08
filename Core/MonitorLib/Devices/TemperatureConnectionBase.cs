using System;

namespace Monitoring
{
	public abstract class TemperatureConnectionBase : LoggableConnection<Double>
	{
		private const int ROUNDING_PRECISION = 3;

		public TemperatureConnectionBase()
		{
		}
		public abstract bool IsCelcius{ get; set;}
		protected internal double ConvertFromC(double inputCelcius)
		{
			if (IsCelcius) {
				return Math.Round(inputCelcius, ROUNDING_PRECISION);
			} else {
				return Math.Round(inputCelcius * 1.8 + 32, ROUNDING_PRECISION);
			}
		}
		protected internal double ConvertFromF(double inputFahrenheit)
		{
			if (IsCelcius) {
				return Math.Round((inputFahrenheit - 32) / 1.8, ROUNDING_PRECISION);
			} else {
				return Math.Round(inputFahrenheit, ROUNDING_PRECISION);
				}
		}

		public double ReadCelcius()
		{
			if (!IsCelcius) 
				return ConvertFromF (ReadValue ());
			else
				return Math.Round(ReadValue (), ROUNDING_PRECISION);
		}
		public double ReadFahrenheit()
		{
			if (IsCelcius) 
				return ConvertFromC (ReadValue ());
			else
				return Math.Round(ReadValue (), ROUNDING_PRECISION);
		}

		public override DeviceType DeviceType
		{
			get {
				return DeviceType.Temperature;
			}
		}
	}
}

