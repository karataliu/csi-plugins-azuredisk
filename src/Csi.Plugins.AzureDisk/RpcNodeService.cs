using System;
using System.Threading.Tasks;
using Csi.V0;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Util.Extensions.Logging.Step;

namespace Csi.Plugins.AzureDisk
{
    sealed class RpcNodeService : Node.NodeBase
    {
        private readonly string nodeId;
        private readonly IAzureDiskAttacher azureDiskAttacher;
        private readonly ILogger logger;

        public RpcNodeService(
            string nodeId,
            IAzureDiskAttacher azureDiskAttacher,
            ILogger<RpcNodeService> logger)
        {
            this.nodeId = nodeId;
            this.azureDiskAttacher = azureDiskAttacher;
            this.logger = logger;

            logger.LogInformation("Node rpc service loaded, nodeId:{0}", nodeId);
        }

        public override async Task<NodePublishVolumeResponse> NodePublishVolume(
            NodePublishVolumeRequest request, ServerCallContext context)
        {
            var id = request.VolumeId;
            var targetPath = request.TargetPath;
            using (var _s = logger.StepInformation("{0}, id: {1}, targetPath: {2}",
                nameof(NodePublishVolume), id, targetPath))
            {
                try
                {
                    if (!request.PublishInfo.TryGetValue("lun", out var lunStr))
                        throw new Exception("No lun info");
                    var lun = int.Parse(lunStr);
                    await azureDiskAttacher.AttachAsync(targetPath, lun);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Exception in AttachAsync");
                    throw;
                }

                _s.Commit();
            }

            await Task.CompletedTask;
            return new NodePublishVolumeResponse();
        }

        public override async Task<NodeUnpublishVolumeResponse> NodeUnpublishVolume(
            NodeUnpublishVolumeRequest request, ServerCallContext context)
        {
            var id = request.VolumeId;
            var targetPath = request.TargetPath;
            using (var _s = logger.StepInformation("{0}, id: {1}, targetPath: {2}",
                nameof(NodeUnpublishVolume), id, targetPath))
            {
                await azureDiskAttacher.DetachAsync(targetPath);
                _s.Commit();
            }

            await Task.CompletedTask;
            return new NodeUnpublishVolumeResponse();
        }

        public override Task<NodeGetIdResponse> NodeGetId(NodeGetIdRequest request, ServerCallContext context)
        {
            var response = new NodeGetIdResponse
            {
                NodeId = this.nodeId,
            };

            return Task.FromResult(response);
        }

        public override Task<NodeGetCapabilitiesResponse> NodeGetCapabilities(
            NodeGetCapabilitiesRequest request,
            ServerCallContext context)
        {
            var response = new NodeGetCapabilitiesResponse { Capabilities = { } };
            return Task.FromResult(response);
        }
    }
}
