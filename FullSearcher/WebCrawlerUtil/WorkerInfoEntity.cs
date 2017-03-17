using System.Diagnostics;
using Microsoft.WindowsAzure.Storage.Table;

namespace WebCrawlerUtil
{
    public class WorkerInfoEntity : TableEntity
    {
        public const string PARTITION_KEY = "workers";

        public WorkerInfoEntity() { }
        public WorkerInfoEntity(string workerName, float cpu, float ram, WorkerState state)
        {
            this.PartitionKey = PARTITION_KEY;
            this.RowKey = workerName;

            this.Name = workerName;
            this.CpuPercent = cpu;
            this.RamOpen = ram;
            this.State = WorkerStateToString(state);
        }

        private string WorkerStateToString(WorkerState state)
        {
            switch (state)
            {
                case WorkerState.IDLE:
                    return "Idle";
                case WorkerState.CRAWLING:
                    return "Crawling";
                case WorkerState.LOADING:
                    return "Loading";
                default:
                    return "Unknown";
            }
        }

        public void UpdateWorkerState(PerformanceCounter cpu, PerformanceCounter ram, WorkerState state)
        {
            this.CpuPercent = cpu.NextValue();
            this.State = WorkerStateToString(state);
            this.RamOpen = ram.NextValue();
        }

        // even though we have floats coming in, tables only accepts doubles
        public double CpuPercent { get; set; }
        public string Name { get; set; }
        public double RamOpen { get; set; }
        public string State { get; set; }
    }

    public enum WorkerState
    {
        IDLE,
        CRAWLING,
        LOADING
    }
}