namespace DACSN10.Areas.Teacher.Service
{
    public static class LiveConfig
    {
        // ==== Server gốc ====
        public const string ApiBase = "https://livestream.nd24.id.vn/api";
        public const string RtmpServer = "rtmp://livestream.nd24.id.vn/live";
        public const string WsEndpoint = "https://livestream.nd24.id.vn/ws"; // WebSocket STOMP endpoint

        // ==== HLS ====
        public const string HlsFormat = "/live/{0}/index.m3u8";

        // ==== STREAM APIs ====
        public static string ApiCreate => $"{ApiBase.TrimEnd('/')}/streams";
        public static string ApiList => $"{ApiBase.TrimEnd('/')}/streams"; // GET list (admin)
        public static string ApiGet(string key) => $"{ApiBase.TrimEnd('/')}/streams/{key}";
        public static string ApiActive(string key, bool active)
            => $"{ApiBase.TrimEnd('/')}/streams/{key}/active?active={active.ToString().ToLower()}";
        public static string ApiPause(string key) => $"{ApiBase.TrimEnd('/')}/streams/{key}/pause";
        public static string ApiResume(string key) => $"{ApiBase.TrimEnd('/')}/streams/{key}/resume";
        public static string ApiEnd(string key) => $"{ApiBase.TrimEnd('/')}/streams/{key}/end";
        public static string ApiDelete(string key) => $"{ApiBase.TrimEnd('/')}/streams/{key}";

        // ==== COMMENT APIs ====
        // GET list comments
        public static string ApiComments(long streamId, long? beforeId = null, int limit = 50)
        {
            var query = $"?limit={limit}";
            if (beforeId.HasValue)
                query += $"&beforeId={beforeId.Value}";
            return $"{ApiBase.TrimEnd('/')}/streams/{streamId}/comments{query}";
        }

        // POST new comment
        public static string ApiPostComment(long streamId)
            => $"{ApiBase.TrimEnd('/')}/streams/{streamId}/comments";

        // ==== WS Comment Topics ====
        public static string WsCommentsTopic(long streamId)
            => $"/topic/streams/{streamId}/comments"; // subscribe để nhận comment mới
        public static string WsSendComment(long streamId)
            => $"/app/streams/{streamId}/comment";    // gửi comment

        // ==== WS Status Topic (pause/resume/end) ====
        public static string WsStatusTopic(string key)
            => $"/topic/streams/{key}/status";

        // ==== HLS helper ====
        public static string Origin
        {
            get
            {
                var noSlash = ApiBase.TrimEnd('/');
                if (noSlash.EndsWith("/api", StringComparison.OrdinalIgnoreCase))
                    return noSlash.Substring(0, noSlash.Length - 4);
                var uri = new Uri(noSlash);
                return uri.GetLeftPart(UriPartial.Authority);
            }
        }

        public static string Api(string path)
            => $"{ApiBase.TrimEnd('/')}/{path.TrimStart('/')}";

        public static string Hls(string streamKey)
            => $"{Origin}{string.Format(HlsFormat, streamKey)}";
    }

    // ===== STREAM SESSION MODEL =====
    public class StreamSession
    {
        public string Title { get; set; } = "Buổi học trực tuyến";
        public string Description { get; set; } = "";
        public string StreamKey { get; set; } = "";
        public long StreamId { get; set; } // ID trong DB (nếu backend trả về)

        public string RtmpServer => LiveConfig.RtmpServer;
        public string HlsUrl => LiveConfig.Hls(StreamKey);

        public string GetWatchUrl(HttpRequest req)
            => $"{req.Scheme}://{req.Host}/watch/{StreamKey}";

        // ==== COMMENT helper ====
        public string GetCommentsApi(long? beforeId = null, int limit = 50)
            => LiveConfig.ApiComments(StreamId, beforeId, limit);

        public string PostCommentApi => LiveConfig.ApiPostComment(StreamId);

        // ==== WebSocket helper ====
        public string WsTopicComments => LiveConfig.WsCommentsTopic(StreamId);
        public string WsSendComment => LiveConfig.WsSendComment(StreamId);
        public string WsStatusTopic => LiveConfig.WsStatusTopic(StreamKey);
        public string WsEndpoint => LiveConfig.WsEndpoint;
    }
}
