import UnityPy
p=r"Gamer Struggles_Data\StreamingAssets\aa\StandaloneWindows64\localization-string-tables-english(en)_assets_all.bundle"
env=UnityPy.load(p)
for o in env.objects:
    print(o.path_id, o.type.name)
    try:
        d=o.read()
        print("  name:", getattr(d,'m_Name',None))
    except Exception as e:
        print("  err", type(e).__name__, e)
