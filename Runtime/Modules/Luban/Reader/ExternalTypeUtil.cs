public static class ExternalTypeUtil
{
    public static XiaoCao.Item ToItem(cfg.Item v)
    {
        return new XiaoCao.Item(v.Type, v.Name, v.Count);
    }

}