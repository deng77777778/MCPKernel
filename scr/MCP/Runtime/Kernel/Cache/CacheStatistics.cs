namespace MCP.Kernel.Cache
{
    /// <summary>
    /// 缓存统计信息
    /// </summary>
    public struct CacheStatistics
    {
        public int SchemaCacheCount { get; set; }
        public int FunctionSchemaCacheCount { get; set; }
        public int ParameterNameCacheCount { get; set; }
        public int AsyncMethodCacheCount { get; set; }
        public int AIContentRelatedCacheCount { get; set; }
        public int TotalCacheCount { get; set; }
        public long CacheMemoryUsage { get; set; }
        public long CacheHits { get; set; }
        public long CacheMisses { get; set; }
        public double HitRate => CacheHits + CacheMisses > 0 ? (double)CacheHits / (CacheHits + CacheMisses) : 0;

        public override string ToString()
        {
            return $"Total: {TotalCacheCount}, Schema: {SchemaCacheCount}, Function: {FunctionSchemaCacheCount}, " +
                   $"Param: {ParameterNameCacheCount}, Async: {AsyncMethodCacheCount}, AI: {AIContentRelatedCacheCount}, " +
                   $"Memory: {CacheMemoryUsage / 1024.0:F2}KB, HitRate: {HitRate:P2}";
        }
    }
}
