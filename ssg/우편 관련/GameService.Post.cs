using System.Collections.Generic;
using System.Linq;
using Latecia.Shared;
using MagicOnion;
using MessagePack;
using Microsoft.EntityFrameworkCore;

public partial class GameService
{
    public async UnaryResult<InventoryResult> RequestPostRewards(List<int> postDataIds)
    {
        var posts = await UserEntry.Collection(u => u.PostDatas).Query().Where(d => postDataIds.Contains(d.Id)).ToListAsync();

        foreach (var post in posts)
        {
            for (int i = 0; i < post.RewardPairs.Count; i++)
            {
                Inventory.GrantRequest.AddItem(Proto.Items.Get(post.RewardPairs[i].Item1).CreateData(post.RewardPairs[i].Item2));
            }
            // update message
            post.SetReceivedRewardComplete();
        }

        // apply rewards
        await Inventory.GrantItemsAsync();
        await DbContext.SaveChangesAsync();

        return Inventory.Result;
    }

    public async UnaryResult<InventoryResult> RequestPostReward(int postDataId)
    {
        var post = await UserEntry.Collection(u => u.PostDatas).Query().Where(d => d.Id == postDataId).SingleAsync();

        for (int i = 0; i < post.RewardPairs.Count; i++)
        {
            var proto = Proto.Items.Get(post.RewardPairs[i].Item1);
            var data = proto.CreateData();
            data.Count = post.RewardPairs[i].Item2;
            Inventory.GrantRequest.AddItem(data);
        }

        // apply rewards
        await Inventory.GrantItemsAsync();

        // update message
        post.SetReceivedRewardComplete();

        await DbContext.SaveChangesAsync();

        return Inventory.Result;
    }

    public async UnaryResult<List<PostData>> RequestRefreshPost(PostType type)
    {
        var user = await UserQuery.Include(u => u.PostDatas).SingleAsync();
        await DeleteOldPost(type);
        var messageList = user.PostDatas.Where(m => m.Type == type).ToList();
        return messageList;
    }

    public async UnaryResult<Nil> RequestDeletePost(int postDataId)
    {
        var user = await UserQuery.Include(u => u.PostDatas).SingleAsync();
        var post = user.PostDatas.Single(m => m.Id == postDataId);
        DbContext.Remove(post);
        await DbContext.SaveChangesAsync();
        return Nil.Default;
    }

    public async UnaryResult<PostData> RequestItemPost(int protoId, long amount)
    {
        var user = await UserQuery.SingleAsync();

        var reqItemProto = ProtoData.Current.Items.Get(protoId);
        var post = new PostData();

        post.SetType(PostType.Manage);
        post.SetTitle("Apply item for test");
        post.SetDescription("Thank you");
        post.SetDeleteTimeBySeconds(6000);
        post.RewardPairs.Add((reqItemProto.Id, amount));
        post.FireTime = ServerTime.GetUTC();

        user.PostDatas ??= new();
        user.PostDatas.Add(post);

        await DeleteOldPost(PostType.Manage);

        await DbContext.SaveChangesAsync();

        return post;
    }

    public async UnaryResult<PostData> RequestSendPost(PostData data)
    {
        var user = await UserQuery.Include(u => u.PostDatas).SingleAsync();

        var post = new PostData();
        var deleteTime = 6000;
        post.SetType(data.Type);
        post.SetTitle(data.TitleText);
        post.SetDescription(data.DescriptionText);
        post.SetDeleteTimeBySeconds(deleteTime);
        post.RewardPairs = data.RewardPairs;
        post.FireTime = ServerTime.GetUTC();
        user.PostDatas.Add(post);

        await DeleteOldPost(PostType.Manage);

        await DbContext.SaveChangesAsync();

        return post;
    }

    async UnaryResult<Nil> DeleteOldPost(PostType type)
    {
        var user = await UserQuery.Include(u => u.PostDatas).SingleAsync();
        var posts = user.PostDatas.Where(m => m.Type == type).ToList();

        if (posts.Count > 200)
        {
            var oldpost = posts.First();
            user.PostDatas.Remove(oldpost);
        }

        await DbContext.SaveChangesAsync();

        return Nil.Default;
    }
}