using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Latecia.Shared;

public partial class GameServiceRequests
{
    public async Task<InventoryResult> RequestPostRewards(List<int> postDataIds)
    {
        var req = await ServerManager.GameService.RequestPostRewards(postDataIds);
        m_UserData.ApplyInventoryResult(req);

        foreach (var id in postDataIds)
        {
            var post = m_UserData.PostData.GetPostDataById(id);
            post.SetReceivedRewardComplete();
        }
        return req;
    }

    public async Task<InventoryResult> RequestPostReward(int postDataId)
    {
        var req = await ServerManager.GameService.RequestPostReward(postDataId);
        m_UserData.ApplyInventoryResult(req);
        
        var post = m_UserData.PostData.GetPostDataById(postDataId);
        post.SetReceivedRewardComplete();
        
        return req;
    }

    public async Task<List<PostData>> RequestRefreshPost(PostType type)
    {
        var req = await ServerManager.GameService.RequestRefreshPost(type);
        var instancePosts = m_UserData.PostData.GetPostDatasByType(type).ToList();

        // add new message
        foreach(var post in req)
        {
            var existPost = instancePosts.FirstOrDefault(m => m.Id == post.Id);
            if (existPost != null) continue;
            m_UserData.PostData.AddPostData(post);
        }

        // remove old message
        foreach (var instancePost in instancePosts)
        {
            var reqPost = req.FirstOrDefault(m => m.Id == instancePost.Id);
            if (reqPost != null) continue;
            m_UserData.PostData.RemovePostData(instancePost.Id);
        }
        
        return instancePosts;
    }

    public async Task RequestDeletePost(int postDataId)
    {
        var req = await ServerManager.GameService.RequestDeletePost(postDataId);
        m_UserData.PostData.RemovePostData(postDataId);
    }

    public async Task<PostData> RequestItemPost(int protoId, long amount)
    {
        var req = await ServerManager.GameService.RequestItemPost(protoId, amount);
        m_UserData.PostData.AddPostData(req);
        return req;
    }

    public async Task<PostData> RequestSendPost(PostData data)
    {
        var req = await ServerManager.GameService.RequestSendPost(data);
        m_UserData.PostData.AddPostData(req);
        return req;
    }
}