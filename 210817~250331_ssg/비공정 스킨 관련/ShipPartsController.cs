public bool ReplaceShipParts(ShipPartsType partsType, ShipSkinType skinType, BundlePath bundlePath, ShipPartsColor? color = null)
{
    var created = CreatePartsObject(bundlePath, partsType, out var info);
    if (created && partsType == ShipPartsType.Body)
    {
        foreach (var kv in m_PartsInfos)
        {
            if (kv.Key == ShipPartsType.Body) continue;
            ChangePartsColor(kv.Value.SkinType, kv.Key, kv.Value.Color);
        }

        foreach (var trans in info.Transforms)
            foreach (var actor in trans.GetComponentsInChildren<IDeckViewActor>())
            {
                actor.Transform.gameObject.SetActive(m_IsDeckViewActorVisible);
            }
    }

    if (color.HasValue)
    {
        ChangePartsColor(skinType, partsType, color.Value);
    }
    else if (created)
    {
        ChangePartsColor(skinType, partsType, m_PartsInfos[partsType].Color);
    }

    if (created) RefreshChildComponents();
    return created;
}

private void RefreshChildComponents()
{
    m_Animators.Clear();
    gameObject.GetComponentsInChildren(m_Animators);
    ShipPartsConditional.CollectObjects(gameObject, m_Conditionals);
}

public void ChangePartsColor(ShipSkinType skinType, ShipPartsType partsType, ShipPartsColor partsColor)
{
    if (!m_PartsInfos.TryGetValue(partsType, out var partsInfo)) return;

    foreach (var rend in partsInfo.Renderers)
    {
        if (rend == null) continue;
        foreach (var material in rend.materials)
        {
            var colorInfo = m_ColorInfo.GetColorInfo(skinType, partsColor);
            var color = colorInfo.GetColor();
            var colorPropertyName = colorInfo.GetColorPropertyName();
            material.SetColor(colorPropertyName, color);
        }
    }

    partsInfo.SkinType = skinType;
    partsInfo.Color = partsColor;
}

bool CreatePartsObject(BundlePath bundlePath, ShipPartsType shipPartsType, out PartsInfo partsInfo)
{
    if (m_PartsInfos.TryGetValue(shipPartsType, out partsInfo))
    {
        if (partsInfo.Prefab.Key == bundlePath.Key) return false;
    }
    else
    {
        partsInfo = new PartsInfo();
        partsInfo.PartsType = shipPartsType;
        m_PartsInfos.Add(shipPartsType, partsInfo);
    }

    partsInfo.Prefab = bundlePath;

    if (shipPartsType == ShipPartsType.Body)
    {
        //clear all, body first presumably
        foreach (var kv in m_PartsInfos) kv.Value.Clear();

        var inst = BundleUtility.InstantiateAsync(partsInfo.Prefab, transform).Wait();
        if (m_IsSkinPage) inst.SetLayer(LayerUtility.CHARACTER, true);
        partsInfo.Transforms.Add(inst.transform);
        partsInfo.Renderers.AddRange(inst.GetComponentsInChildren<Renderer>());

        foreach (var kv in m_PartsInfos)
        {
            if (kv.Key == ShipPartsType.Body) continue;
            _CreateParts(kv.Value, inst.transform, m_IsSkinPage);
        }
    }
    else if (m_PartsInfos.TryGetValue(ShipPartsType.Body, out var bodyInfo) && bodyInfo.Transforms.Count > 0)
    {
        partsInfo.Clear();
        _CreateParts(partsInfo, bodyInfo.Transforms[0], m_IsSkinPage);
    }

    return true;

    static void _CreateParts(PartsInfo info, Transform bodyTransform, bool skinPage)
    {
        foreach (var kv in s_BoneReference[info.PartsType])
        {
            var boneName = kv.name;
            var partPath = info.Prefab.AppendPath(kv.suffix);
            var parentTransform = _FindRecursive(boneName, bodyTransform);
            if (parentTransform == null) continue; //don't spawn if no parent
            var inst = BundleUtility.InstantiateAsync(partPath, parentTransform).Wait();
            if (skinPage) inst.SetLayer(LayerUtility.CHARACTER, true);
            info.Transforms.Add(inst.transform);
            info.Renderers.AddRange(inst.GetComponentsInChildren<Renderer>());
        }
    }

    static Transform _FindRecursive(string match, Transform trans)
    {
        if (trans.gameObject.name == match) return trans;
        for (int i = 0; i < trans.childCount; i++)
        {
            var child = _FindRecursive(match, trans.GetChild(i));
            if (child != null) return child;
        }
        return null;
    }
}

public void SetDefaultParts(ShipPartsType shipPartsType, int shipLevel)
{
    var defaultPartsProto = ProtoData.Current.Items.Where(i => i.InventoryType == ItemInventoryType.Shipskin)
                                                                .Where(i => i.As<ShipSkinPartsProto>().Level == shipLevel)
                                                                .Single(i => i.As<ShipSkinPartsProto>().ShipPartsType == shipPartsType)
                                                                .As<ShipSkinPartsProto>();

    ReplaceShipParts(shipPartsType, ShipSkinType.Default, defaultPartsProto.Prefab, ShipPartsColor.Default);
}