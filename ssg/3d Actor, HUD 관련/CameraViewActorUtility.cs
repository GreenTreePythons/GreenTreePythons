using UnityEngine;
using SingleSquadBattle;

public static class CameraViewActorUtility
{
    public static CharacterActor ShowCharacterActor(BundlePath bundlePath, Transform actorRootTransform)
        => SetCharacterActor(bundlePath, actorRootTransform);

    public static GameObject ShowActor(BundlePath bundlePath, Transform actorRootTransform) 
        => SetActor(bundlePath, actorRootTransform);

    public static CharacterActor ShowCharacterActorWithAnimation(BundlePath bundlePath, Transform actorRootTransform, string animationName)
    {
        var actor = SetCharacterActor(bundlePath, actorRootTransform);
        PlayCharacterActorAnimation(actor, animationName);
        return actor;
    }

    public static void PlayCharacterActorAnimation(CharacterActor actor, string animationName)
    {
        var battleActor = actor.GetComponent<ClientBattleActor>();
        battleActor.Animator.DefaultLayer.Play(animationName, updateImmediate: true);
        battleActor.Animator.BaseLayer.Play(BattleAnim.Stand1.Value, updateImmediate: true);
    }

    static GameObject SetActor(BundlePath bundlePath, Transform actorRootTransform)
    {
        var go = BundleUtility.InstantiateAsync(bundlePath).Wait();
        go.transform.SetParent(actorRootTransform);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        go.SetLayer(LayerUtility.CHARACTER, true);
        return go;
    }

    static CharacterActor SetCharacterActor(BundlePath bundlePath, Transform actorRootTransform)
    {
        var actor = SetActor(bundlePath, actorRootTransform);
        var characterActor = actor.AddComponent<CharacterActor>();
        var battleActor = characterActor.GetComponent<ClientBattleActor>();
        if (battleActor != null)
        {
            characterActor.transform.localPosition -= Vector3.up * battleActor.DefaultHeight;
        }

        return characterActor;
    }
}