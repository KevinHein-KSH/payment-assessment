global using Xunit;

// Collections share one MySQL and Redis, and each starts its own API (migrations run at startup): run them one at a time
[assembly: CollectionBehavior(DisableTestParallelization = true)]
