# MCPKernel

一个轻量级、模块化的 MCP，专注于提供核心基础设施能力，同时保持最小依赖。这个库只实现了核心功能，没有界面也没有Tool，Resouces,Prompt 这些具体功能将会在了另一个库中。

## 🎯 设计理念

- **极简依赖**：仅依赖 `Newtonsoft.Json`
- **模块化设计**：各功能模块独立，按需使用
- **最佳实践借鉴**：吸收业界优秀框架的设计思想，保持 API 亲切感

## 📦 模块说明

### DependencyInjection - 依赖注入
借鉴自 `Microsoft.Extensions.DependencyInjection`，保留核心接口：
- `IServiceCollection` - 服务注册
- `IServiceProvider` - 服务解析


### Protocol - 协议通信
参考 `ModelContextProtocol.Protocol` 实现，提供：
- 标准化的协议通信基础
- 可扩展的消息处理机制

### Results - 结果封装
采用 ASP.NET Core 风格的结果封装：
- 统一的结果返回格式
- 支持成功/失败状态

### MiniTask - 轻量级任务调度
受 `UniTask` 启发，实现轻量级异步操作：
- `SwitchToThreadPool` - 切换到线程池
- `SwitchToMainThread` - 切换到主线程  
- `Yield` - 让出当前执行

**特性**：
- Editor 和 Runtime 无缝切换
- 零分配异步操作

### AIFunction - AI 方法抽象
参考 `Microsoft.Extensions.AI` 设计：
- 统一的 AI 方法调用接口
- 支持多种 AI 服务扩展

### Schema - JSON 序列化
- 使用 `Newtonsoft.Json` 替代 `System.Text.Json`
- 提供与微软生态一致的 API 体验

## 🚀 快速开始

### 安装
PackageManager中选择add package from git URL
```bash
https://github.com/deng77777778/MCPKernel.git?path=/scr/MCP
