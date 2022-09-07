using System;
using ColossalFramework;
using ICities;
using UnityEngine;

namespace Inflation {
	public class InflationMod : IUserMod, IEconomyExtension {
		public string Name => "Inflation";
		public string Description => "Add inflation to all construction and maintenance cost";

		public void OnSettingsUI(UIHelperBase helper) {
			var group = helper.AddGroup("Parameters");
			group.AddDropdown("Inflation Period", Enum.GetNames(typeof(InflationPeriod)), (int) InflationPeriod.Monthly,
				value => SelectedInflationPeriod = (InflationPeriod) value);
			group.AddTextfield($"Construction Annual Inflation Rate", "0.05", value => {
				var isFloat = float.TryParse(value, out var parsed);
				ConstructionAnnualInflationRate = isFloat ? parsed : 0.05f;
			});
			group.AddTextfield($"Maintenance Annual Inflation Rate", "0.01", value => {
				var isFloat = float.TryParse(value, out var parsed);
				MaintenanceAnnualInflationRate = isFloat ? parsed : 0.01f;
			});
			group.AddCheckbox("Compound Inflation", true, value => CompoundInflation = value);
		}

		public enum InflationType {
			Construction,
			Maintenance
		}

		public enum InflationPeriod {
			Daily,
			Weekly,
			Monthly,
			Yearly
		}
		
		public float ConstructionAnnualInflationRate { get; set; } = 0.05f;
		public float MaintenanceAnnualInflationRate { get; set; } = 0.01f;
		public InflationPeriod SelectedInflationPeriod { get; set; } = InflationPeriod.Monthly;
		public bool CompoundInflation { get; set; } = true;
		

		private SimulationManager _simulationManager;

		int GetInflatedCost(InflationType inflationType, int originalCost) {
			var numberOfPeriods = GetNumberOfPeriodsSinceStart();
			var periodsInYear = GetPeriodsInYear();

			var inflationRateInPeriod = inflationType == InflationType.Construction
				? ConstructionAnnualInflationRate / periodsInYear
				: MaintenanceAnnualInflationRate / periodsInYear;
			
			if (!CompoundInflation) return (int) (numberOfPeriods * inflationRateInPeriod * originalCost + originalCost);

			var compoundedInflation = Mathf.Pow(1 + inflationRateInPeriod, numberOfPeriods);
			return (int) (originalCost * compoundedInflation);
		}

		int GetNumberOfPeriodsSinceStart() {
			var currentTime = _simulationManager.m_currentGameTime;
			var startTime = _simulationManager.m_metaData.m_startingDateTime;

			return (int) Mathf.Floor((float) (currentTime - startTime).TotalDays / GetPeriodDivider());
		}

		int GetPeriodDivider() {
			switch (SelectedInflationPeriod) {
				case InflationPeriod.Daily:
					return 1;
				case InflationPeriod.Weekly:
					return 7;
				case InflationPeriod.Monthly:
					return 30;
				case InflationPeriod.Yearly:
					return 365;
			}

			return 7;
		}

		int GetPeriodsInYear() {
			switch (SelectedInflationPeriod) {
				case InflationPeriod.Daily:
					return 365;
				case InflationPeriod.Weekly:
					return 52;
				case InflationPeriod.Monthly:
					return 12;
				case InflationPeriod.Yearly:
					return 1;
			}

			return 52;
		}


		public void OnCreated(IEconomy economy) {
			_simulationManager = Singleton<SimulationManager>.instance;
		}

		public void OnReleased() { }

		public long OnUpdateMoneyAmount(long internalMoneyAmount) {
			return internalMoneyAmount;
		}

		public int OnPeekResource(EconomyResource resource, int amount) {
			return amount;
		}

		public int OnFetchResource(EconomyResource resource, int amount, Service service, SubService subService,
			Level level) {
			return amount;
		}

		public int OnAddResource(EconomyResource resource, int amount, Service service, SubService subService,
			Level level) {
			return amount;
		}

		public int OnGetConstructionCost(int originalConstructionCost, Service service, SubService subService,
			Level level) {
			return GetInflatedCost(InflationType.Construction, originalConstructionCost);
		}

		public int OnGetMaintenanceCost(int originalMaintenanceCost, Service service, SubService subService,
			Level level) {
			return GetInflatedCost(InflationType.Maintenance, originalMaintenanceCost);
		}

		public int OnGetRelocationCost(int constructionCost, int relocationCost, Service service, SubService subService,
			Level level) {
			return relocationCost;
		}

		public int OnGetRefundAmount(int constructionCost, int refundAmount, Service service, SubService subService,
			Level level) {
			return refundAmount;
		}

		public bool OverrideDefaultPeekResource { get; }
	}
}