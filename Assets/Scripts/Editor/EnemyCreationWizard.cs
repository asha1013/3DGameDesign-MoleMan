using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEngine.AI;

public class EnemyCreationWizard : EditorWindow
{
    // Wizard state
    private enum WizardPage
    {
        Name,
        BaseSprite,
        AdjustBase,
        MoveSprite,
        WindupQuestion,
        WindupSprite,
        AdjustWindup,
        AttackSprite,
        AdjustAttack,
        EnemySize,
        AttackType,
        BasicStats,
        AttackTiming,
        StraferSettings,
        SoundEffects,
        Drops,
        Finalize
    }

    private WizardPage currentPage = WizardPage.Name;
    private Vector2 scrollPosition;

    // Name
    private string enemyName = "";

    // Sprites and textures
    private Texture2D baseSprite;
    private Texture2D glowSprite;
    private Texture2D emissionMap;
    private Color glowColor = Color.white;
    private Texture2D moveSprite;
    private bool moveDifferentHitbox = false;
    private Texture2D windupSprite;
    private bool windupDifferentHitbox = false;
    private Texture2D attackSprite;
    private bool attackDifferentHitbox = false;
    private bool reuseAttackForWindup = false;
    private float enemySize = 1f;

    // Attack type
    private EnemyAttack.AttackType attackType = EnemyAttack.AttackType.Melee;

    // Basic stats
    private int maxHP = 5;
    private float moveSpeed = 3f;
    private int damage = 1;
    private float range = 1.5f;
    private float moveAnimRate = 0.4f;

    // Attack timing
    private float delay = 0.5f;
    private float duration = 0.3f;
    private float cooldown = 1f;
    private bool moveAttack = false;

    // Strafer settings
    private bool isStrafe = false;
    private float strafeDistance = 3f;
    private bool isSneaky = false;
    private float sneakAngle = 110f;

    // Melee specific
    private bool meleeSpecificHitbox = false;

    // Projectile specific
    private float projectileSpeed = 10f;
    private bool usingGravity = false;
    private float arcHeight = 3f;
    private float dropOff = 1f;
    private bool isAOE = false;
    private float splashRadius = 2f;
    private bool projectileHasMesh = false;
    private Mesh projectileMesh;
    private bool projectileHasSprite = false;
    private Texture2D projectileSprite;
    private bool hasParticles = false;
    private bool hasHitParticles = false;

    // Lunging specific
    private bool lungeJumps = false;
    private float jumpHeight = 3f;
    private float lungeDistance = 5f;
    private float bounceBack = 2f;
    private bool lungeSpecificHitbox = false;

    // Sound effects
    private bool hasAgroSound = false;
    private AudioClip agroClip;
    private bool hasDieSound = false;
    private AudioClip dieClip;
    private bool hasMoveSound = false;
    private AudioClip moveSFX;
    private bool hasWindupSound = false;
    private AudioClip windupClip;
    private bool hasAttackSound = false;
    private AudioClip attackClip;
    private bool hasDamagedSound = false;
    private AudioClip damagedSFX;
    private bool hasAttackHitSound = false;
    private AudioClip attackHitSFX;
    private bool hasProjectileHitSound = false;
    private AudioClip projectileHitSFX;

    // Drops
    private bool hasDrops = false;
    private GameObject enemyDrop;

    // Create Drop Material page
    private string dropMaterialName = "";
    private Texture2D dropTexture;
    private Texture2D dropIcon;

    // Created objects
    private GameObject enemyPrefab;
    private GameObject bodyObject;
    private GameObject idleChild;
    private GameObject moveChild;
    private GameObject windupChild;
    private GameObject attackChild;
    private GameObject glowPart;
    private GameObject projectileOriginMarker;
    private Material idleMaterial;
    private Material glowMaterial;
    private Material moveMaterial;
    private Material windupMaterial;
    private Material attackMaterial;

    [MenuItem("Tools/Enemy Creation Wizard")]
    static void CreateWizard()
    {
        EnemyCreationWizard window = GetWindow<EnemyCreationWizard>("Enemy Creation Wizard");
        window.minSize = new Vector2(600, 700);
        window.Show();
    }

    void OnGUI()
    {
        // Add scroll view to prevent text cutoff
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // Draw only the current page - clean single-page view
        switch (currentPage)
        {
            case WizardPage.Name:
                DrawNamePage();
                break;
            case WizardPage.BaseSprite:
                DrawBaseSpritePage();
                break;
            case WizardPage.AdjustBase:
                DrawAdjustBasePage();
                break;
            case WizardPage.MoveSprite:
                DrawMoveSpritePage();
                break;
            case WizardPage.WindupQuestion:
                DrawWindupQuestionPage();
                break;
            case WizardPage.WindupSprite:
                DrawWindupSpritePage();
                break;
            case WizardPage.AdjustWindup:
                DrawAdjustWindupPage();
                break;
            case WizardPage.AttackSprite:
                DrawAttackSpritePage();
                break;
            case WizardPage.AdjustAttack:
                DrawAdjustAttackPage();
                break;
            case WizardPage.EnemySize:
                DrawEnemySizePage();
                break;
            case WizardPage.AttackType:
                DrawAttackTypePage();
                break;
            case WizardPage.BasicStats:
                DrawBasicStatsPage();
                break;
            case WizardPage.AttackTiming:
                DrawAttackTimingPage();
                break;
            case WizardPage.StraferSettings:
                DrawStraferSettingsPage();
                break;
            case WizardPage.SoundEffects:
                DrawSoundEffectsPage();
                break;
            case WizardPage.Drops:
                DrawDropsPage();
                break;
            case WizardPage.Finalize:
                DrawFinalizePage();
                break;
        }

        EditorGUILayout.EndScrollView();

        // Navigation buttons at bottom (outside scroll view)
        EditorGUILayout.Space(10);
        EditorGUILayout.BeginHorizontal();

        // Back button
        if (currentPage > WizardPage.Name)
        {
            if (GUILayout.Button("Back", GUILayout.Height(30), GUILayout.Width(100)))
            {
                OnBackButton();
            }
        }

        GUILayout.FlexibleSpace();

        // Next/Finish button
        string buttonText = currentPage == WizardPage.Finalize ? "Finish" : "Next";
        if (GUILayout.Button(buttonText, GUILayout.Height(30), GUILayout.Width(100)))
        {
            OnNextButton();
        }

        EditorGUILayout.EndHorizontal();
    }

    void DrawNamePage()
    {
        EditorGUILayout.HelpBox("Welcome to the Enemy Creation Wizard!\n\nThis wizard will guide you through creating a new enemy prefab.", MessageType.Info);
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("What is your Enemy's name?");
        enemyName = EditorGUILayout.TextField(enemyName);
    }

    void DrawBaseSpritePage()
    {
        EditorGUILayout.LabelField("States and Sprites", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Base Enemy Sprite (without glowing part)");
        baseSprite = (Texture2D)EditorGUILayout.ObjectField(baseSprite, typeof(Texture2D), false);

        if (baseSprite != null)
        {
            DrawTexturePreview(baseSprite, 300, 400);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Enemy glowing part sprite");
        glowSprite = (Texture2D)EditorGUILayout.ObjectField(glowSprite, typeof(Texture2D), false);

        EditorGUILayout.LabelField("Enemy signature/glow color");
        glowColor = EditorGUILayout.ColorField(glowColor);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Emission Map (optional)");
        emissionMap = (Texture2D)EditorGUILayout.ObjectField(emissionMap, typeof(Texture2D), false);
        EditorGUILayout.HelpBox("Emission Map: A texture that controls which parts of the sprite glow and how intensely. White areas glow at full intensity, black areas don't glow. Create one by duplicating your glow sprite and adjusting brightness/contrast, or painting white on areas you want to glow brightest.", MessageType.Info);
    }

    void DrawAdjustBasePage()
    {
        EditorGUILayout.HelpBox("Adjust the position, rotation, and scale of the GlowPart and colliders in the scene.\n\nThe objects are selected in the hierarchy for you.", MessageType.Info);
        EditorGUILayout.Space();

        if (GUILayout.Button("Select GlowPart"))
        {
            SelectAndFrameObject(glowPart);
        }

        if (GUILayout.Button("Select Capsule Collider"))
        {
            SelectAndFrameObject(idleChild);
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Click Next when you're satisfied with the adjustments.", MessageType.Info);
    }

    void DrawMoveSpritePage()
    {
        EditorGUILayout.LabelField("Movement Sprite", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Attach an alternate sprite for movement animation (stepping, flapping, etc.).\nThe animation will cycle between idle and move sprites.", MessageType.Info);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Alternate Enemy Sprite for movement");
        moveSprite = (Texture2D)EditorGUILayout.ObjectField(moveSprite, typeof(Texture2D), false);

        if (moveSprite != null && baseSprite != null)
        {
            EditorGUILayout.Space();
            DrawSideBySidePreview(baseSprite, moveSprite, "Idle Sprite", "Move Sprite");
        }

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Is the silhouette different enough to warrant a differently shaped or sized hitbox?");
        moveDifferentHitbox = EditorGUILayout.Toggle(moveDifferentHitbox);

        EditorGUILayout.LabelField("Animation cycle speed (seconds)");
        moveAnimRate = EditorGUILayout.Slider(moveAnimRate, 0.1f, 1f);
    }

    void DrawWindupQuestionPage()
    {
        // Simplified - combine into one page
        EditorGUILayout.LabelField("Windup Sprite", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Attach the sprite to display when the enemy is winding up to attack.\nNote: You can leave this empty to use the idle sprite, or use the same sprite as attack.", MessageType.Info);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Windup Enemy Sprite (optional)");
        windupSprite = (Texture2D)EditorGUILayout.ObjectField(windupSprite, typeof(Texture2D), false);

        if (windupSprite != null && baseSprite != null)
        {
            EditorGUILayout.Space();
            DrawSideBySidePreview(baseSprite, windupSprite, "Idle Sprite", "Windup Sprite");
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Different silhouette/hitbox?");
        windupDifferentHitbox = EditorGUILayout.Toggle(windupDifferentHitbox);
    }

    void DrawWindupSpritePage()
    {
        // This page is no longer used - keeping for compatibility
        DrawWindupQuestionPage();
    }

    void DrawAdjustWindupPage()
    {
        EditorGUILayout.HelpBox("Adjust the windup state objects in the scene.", MessageType.Info);
        EditorGUILayout.Space();

        if (windupChild != null && GUILayout.Button("Select Windup GlowPart"))
        {
            Transform glow = windupChild.transform.Find("GlowPart");
            if (glow != null) SelectAndFrameObject(glow.gameObject);
        }

        if (windupChild != null && GUILayout.Button("Select Windup Collider"))
        {
            SelectAndFrameObject(windupChild);
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Click Next when you're satisfied with the adjustments.", MessageType.Info);
    }

    void DrawAttackSpritePage()
    {
        EditorGUILayout.LabelField("Attack Sprite", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Attach the sprite to display when the enemy is actively attacking.", MessageType.Info);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Attack Enemy Sprite");
        attackSprite = (Texture2D)EditorGUILayout.ObjectField(attackSprite, typeof(Texture2D), false);

        if (attackSprite != null && baseSprite != null)
        {
            EditorGUILayout.Space();
            DrawSideBySidePreview(baseSprite, attackSprite, "Idle Sprite", "Attack Sprite");
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Different silhouette/hitbox?");
        attackDifferentHitbox = EditorGUILayout.Toggle(attackDifferentHitbox);

        if (attackDifferentHitbox)
        {
            EditorGUILayout.HelpBox("A separate child GameObject will be created with its own collider.", MessageType.Info);
        }
    }

    void DrawAdjustAttackPage()
    {
        EditorGUILayout.HelpBox("Adjust the attack state colliders in the scene.", MessageType.Info);
        EditorGUILayout.Space();

        if (attackChild != null && GUILayout.Button("Select Attack Collider"))
        {
            SelectAndFrameObject(attackChild);
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Click Next when you're satisfied with the adjustments.", MessageType.Info);
    }

    void DrawWindupReusePage()
    {
        EditorGUILayout.LabelField("Windup Configuration", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("If you want the attack sprite to also be used for windup (instead of idle), enable this option.", MessageType.Info);
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Use attack sprite for windup?");
        reuseAttackForWindup = EditorGUILayout.Toggle(reuseAttackForWindup);
    }

    void DrawEnemySizePage()
    {
        EditorGUILayout.LabelField("Enemy Size", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Adjust the size of the enemy relative to the player (1.0 = same size).", MessageType.Info);
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Size (compared to player)");
        enemySize = EditorGUILayout.Slider(enemySize, 0.25f, 2.5f);
    }

    void DrawAttackTypePage()
    {
        EditorGUILayout.LabelField("Attack Type", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Select the type of attack this enemy will use.", MessageType.Info);
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Attack Type");
        attackType = (EnemyAttack.AttackType)EditorGUILayout.EnumPopup(attackType);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Attack Configuration", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Show attack-specific settings based on selection
        switch (attackType)
        {
            case EnemyAttack.AttackType.Melee:
                DrawMeleeSpecific();
                break;
            case EnemyAttack.AttackType.Projectile:
                DrawProjectileSpecific();
                break;
            case EnemyAttack.AttackType.Lunging:
                DrawLungingSpecific();
                break;
            case EnemyAttack.AttackType.Unique:
                EditorGUILayout.HelpBox("You must implement the IUniqueAttack interface yourself for unique attacks.", MessageType.Info);
                break;
        }
    }

    void DrawBasicStatsPage()
    {
        EditorGUILayout.LabelField("Basic Stats", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Max HP");
        maxHP = EditorGUILayout.IntField(maxHP);

        EditorGUILayout.LabelField("Movement Speed");
        moveSpeed = EditorGUILayout.FloatField(moveSpeed);

        EditorGUILayout.LabelField("Attack Damage");
        damage = EditorGUILayout.IntField(damage);

        // Set default range based on attack type
        float defaultRange = attackType == EnemyAttack.AttackType.Projectile ? 8f :
                            attackType == EnemyAttack.AttackType.Lunging ? 5f :
                            attackType == EnemyAttack.AttackType.Melee ? 1.5f : 3f;

        if (range == 0) range = defaultRange;
        EditorGUILayout.LabelField("Attack Range");
        range = EditorGUILayout.FloatField(range);
    }

    void DrawAttackTimingPage()
    {
        EditorGUILayout.LabelField("Attack Timing", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Windup Delay (seconds) - Time before attack executes");
        delay = EditorGUILayout.FloatField(delay);

        EditorGUILayout.LabelField("Attack Duration (seconds) - How long hitbox stays active");
        duration = EditorGUILayout.FloatField(duration);

        EditorGUILayout.LabelField("Cooldown (seconds) - Time between attacks");
        cooldown = EditorGUILayout.FloatField(cooldown);

        EditorGUILayout.LabelField("Can move during attack?");
        moveAttack = EditorGUILayout.Toggle(moveAttack);
    }

    void DrawStraferSettingsPage()
    {
        EditorGUILayout.LabelField("Movement AI Settings", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox("Configure how the enemy moves in combat. Choose between strafing around the player or sneaky flanking behavior.\n\nNote: These behaviors are mutually exclusive. Enabling one will disable the other.", MessageType.Info);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Strafe Movement");
        EditorGUILayout.HelpBox("Enemy will circle around the player at a set distance during combat.", MessageType.None);
        bool newStrafe = EditorGUILayout.Toggle("Enable Strafe", isStrafe);

        if (newStrafe && !isStrafe)
        {
            // User just enabled strafe, disable sneaky
            isSneaky = false;
        }
        isStrafe = newStrafe;

        if (isStrafe)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Strafe Distance (default: 3)");
            strafeDistance = EditorGUILayout.FloatField(strafeDistance);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Sneaky/Flanking Movement");
        EditorGUILayout.HelpBox("Enemy will attempt to flank the player by staying at an angle from their view.", MessageType.None);
        bool newSneaky = EditorGUILayout.Toggle("Enable Sneaky/Flanking", isSneaky);

        if (newSneaky && !isSneaky)
        {
            // User just enabled sneaky, disable strafe
            isStrafe = false;
        }
        isSneaky = newSneaky;

        if (isSneaky)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Flank Angle (default: 110)");
            sneakAngle = EditorGUILayout.FloatField(sneakAngle);
            EditorGUI.indentLevel--;
        }
    }

    void DrawAttackTypeSpecificPage()
    {
        EditorGUILayout.LabelField("Attack Type Specific", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        switch (attackType)
        {
            case EnemyAttack.AttackType.Melee:
                DrawMeleeSpecific();
                break;
            case EnemyAttack.AttackType.Projectile:
                DrawProjectileSpecific();
                break;
            case EnemyAttack.AttackType.Lunging:
                DrawLungingSpecific();
                break;
            case EnemyAttack.AttackType.Unique:
                EditorGUILayout.HelpBox("You must implement the IUniqueAttack interface yourself for unique attacks.", MessageType.Info);
                break;
        }
    }

    void DrawMeleeSpecific()
    {
        EditorGUILayout.LabelField("Melee Configuration", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Use specific hitbox part? (Create separate hitbox child for precise damage detection)");
        meleeSpecificHitbox = EditorGUILayout.Toggle(meleeSpecificHitbox);

        if (meleeSpecificHitbox)
        {
            EditorGUILayout.HelpBox("A separate hitbox child will be created that you can position and size.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("The entire attack child collider will deal damage.", MessageType.Info);
        }
    }

    void DrawProjectileSpecific()
    {
        EditorGUILayout.LabelField("Projectile Configuration", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Projectile Speed (default: 10)");
        projectileSpeed = EditorGUILayout.FloatField(projectileSpeed);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Does the projectile travel in an arc?");
        usingGravity = EditorGUILayout.Toggle(usingGravity);
        if (usingGravity)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Arc Height (default: 3)");
            arcHeight = EditorGUILayout.FloatField(arcHeight);
            EditorGUI.indentLevel--;
        }
        else
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Drop-off / Gravity (default: 0 for straight)");
            dropOff = EditorGUILayout.FloatField(dropOff);
            EditorGUILayout.HelpBox("Drop-off controls how much the projectile falls during flight. 0 = perfectly straight, higher values = more drop.", MessageType.Info);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Does the projectile deal damage in an area after collision?");
        isAOE = EditorGUILayout.Toggle(isAOE);
        if (isAOE)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Splash Radius");
            splashRadius = EditorGUILayout.FloatField(splashRadius);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Does the projectile use a sprite?");
        projectileHasSprite = EditorGUILayout.Toggle(projectileHasSprite);
        if (projectileHasSprite)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Projectile Sprite Texture");
            projectileSprite = (Texture2D)EditorGUILayout.ObjectField(projectileSprite, typeof(Texture2D), false);
            EditorGUI.indentLevel--;
        }
        else
        {
            EditorGUILayout.LabelField("Does the projectile have a mesh?");
            projectileHasMesh = EditorGUILayout.Toggle(projectileHasMesh);
            if (projectileHasMesh)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("Projectile Mesh");
                projectileMesh = (Mesh)EditorGUILayout.ObjectField(projectileMesh, typeof(Mesh), false);
                if (projectileMesh == null)
                {
                    EditorGUILayout.HelpBox("Will default to generic sphere (0.1 scale) if no mesh provided.", MessageType.Info);
                }
                EditorGUI.indentLevel--;
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Does the projectile emit particles when traveling?");
        hasParticles = EditorGUILayout.Toggle(hasParticles);
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Does the projectile emit particles on collision?");
        hasHitParticles = EditorGUILayout.Toggle(hasHitParticles);

        if (hasParticles || hasHitParticles)
        {
            EditorGUILayout.HelpBox("Particle systems will be created automatically. You can customize them in the prefab afterward.", MessageType.Info);
        }
    }

    void DrawLungingSpecific()
    {
        EditorGUILayout.LabelField("Lunging Configuration", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Jump during attack?");
        lungeJumps = EditorGUILayout.Toggle(lungeJumps);
        if (lungeJumps)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Jump Height (Default: 3)");
            jumpHeight = EditorGUILayout.FloatField(jumpHeight);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.LabelField("Lunge Distance (Default: 5)");
        lungeDistance = EditorGUILayout.FloatField(lungeDistance);

        EditorGUILayout.LabelField("Bounce Back Distance (Default: 2)");
        bounceBack = EditorGUILayout.FloatField(bounceBack);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Use specific hitbox part?");
        lungeSpecificHitbox = EditorGUILayout.Toggle(lungeSpecificHitbox);

        if (lungeSpecificHitbox)
        {
            EditorGUILayout.HelpBox("A separate hitbox child will be created that you can position and size.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("The entire attack child collider will deal damage.", MessageType.Info);
        }
    }

    void DrawSoundEffectsPage()
    {
        EditorGUILayout.LabelField("Sound Effects", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Add audio clips for various enemy actions (all optional).", MessageType.Info);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Agro Sound");
        hasAgroSound = EditorGUILayout.Toggle(hasAgroSound);
        if (hasAgroSound)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Audio Clip");
            agroClip = (AudioClip)EditorGUILayout.ObjectField(agroClip, typeof(AudioClip), false);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.LabelField("Die Sound");
        hasDieSound = EditorGUILayout.Toggle(hasDieSound);
        if (hasDieSound)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Audio Clip");
            dieClip = (AudioClip)EditorGUILayout.ObjectField(dieClip, typeof(AudioClip), false);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.LabelField("Move Sound");
        hasMoveSound = EditorGUILayout.Toggle(hasMoveSound);
        if (hasMoveSound)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Audio Clip");
            moveSFX = (AudioClip)EditorGUILayout.ObjectField(moveSFX, typeof(AudioClip), false);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.LabelField("Windup Sound");
        hasWindupSound = EditorGUILayout.Toggle(hasWindupSound);
        if (hasWindupSound)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Audio Clip");
            windupClip = (AudioClip)EditorGUILayout.ObjectField(windupClip, typeof(AudioClip), false);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.LabelField("Attack Sound");
        hasAttackSound = EditorGUILayout.Toggle(hasAttackSound);
        if (hasAttackSound)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Audio Clip");
            attackClip = (AudioClip)EditorGUILayout.ObjectField(attackClip, typeof(AudioClip), false);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.LabelField("Damaged Sound");
        hasDamagedSound = EditorGUILayout.Toggle(hasDamagedSound);
        if (hasDamagedSound)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Audio Clip");
            damagedSFX = (AudioClip)EditorGUILayout.ObjectField(damagedSFX, typeof(AudioClip), false);
            EditorGUI.indentLevel--;
        }

        if (attackType == EnemyAttack.AttackType.Melee || attackType == EnemyAttack.AttackType.Lunging)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Attack Hit Sound");
            hasAttackHitSound = EditorGUILayout.Toggle(hasAttackHitSound);
            if (hasAttackHitSound)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("Audio Clip");
                attackHitSFX = (AudioClip)EditorGUILayout.ObjectField(attackHitSFX, typeof(AudioClip), false);
                EditorGUI.indentLevel--;
            }
        }

        if (attackType == EnemyAttack.AttackType.Projectile)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Projectile Hit Sound");
            hasProjectileHitSound = EditorGUILayout.Toggle(hasProjectileHitSound);
            if (hasProjectileHitSound)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("Audio Clip");
                projectileHitSFX = (AudioClip)EditorGUILayout.ObjectField(projectileHitSFX, typeof(AudioClip), false);
                EditorGUI.indentLevel--;
            }
        }
    }

    void DrawDropsPage()
    {
        EditorGUILayout.LabelField("Item Drops", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Configure what item this enemy drops when killed.", MessageType.Info);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Use existing drop prefab?");
        bool useExistingPrefab = EditorGUILayout.Toggle(hasDrops && enemyDrop != null);

        if (useExistingPrefab)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Item Prefab");
            enemyDrop = (GameObject)EditorGUILayout.ObjectField(enemyDrop, typeof(GameObject), false);
            hasDrops = (enemyDrop != null);
            EditorGUI.indentLevel--;
        }
        else
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Create New Material Drop", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Create a new material drop for this enemy.", MessageType.Info);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Material Name");
            dropMaterialName = EditorGUILayout.TextField(dropMaterialName);

            EditorGUILayout.LabelField("Texture (PNG)");
            dropTexture = (Texture2D)EditorGUILayout.ObjectField(dropTexture, typeof(Texture2D), false);

            EditorGUILayout.LabelField("Icon (optional, uses texture if not set)");
            dropIcon = (Texture2D)EditorGUILayout.ObjectField(dropIcon, typeof(Texture2D), false);

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Clicking 'Next' will create:\n- Material from texture\n- Item ScriptableObject\n- Drop prefab in Drop subfolder", MessageType.Info);
        }
    }

    void DrawFinalizePage()
    {
        EditorGUILayout.HelpBox("Ready to finalize the enemy prefab!\n\nClick 'Finish' to complete the setup.", MessageType.Info);
        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"Enemy Name: {enemyName}");
        EditorGUILayout.LabelField($"Attack Type: {attackType}");
        EditorGUILayout.LabelField($"Max HP: {maxHP}");
        EditorGUILayout.LabelField($"Move Speed: {moveSpeed}");
    }

    void OnNextButton()
    {
        // Handle page progression with conditional logic
        switch (currentPage)
        {
            case WizardPage.Name:
                if (string.IsNullOrEmpty(enemyName))
                {
                    EditorUtility.DisplayDialog("Error", "Please enter an enemy name.", "OK");
                    return;
                }
                CreateInitialSetup();
                currentPage = WizardPage.BaseSprite;
                break;

            case WizardPage.BaseSprite:
                if (baseSprite == null || glowSprite == null)
                {
                    EditorUtility.DisplayDialog("Error", "Please assign both base and glow sprites.", "OK");
                    return;
                }
                ApplyBaseSprites();
                currentPage = WizardPage.AdjustBase;
                break;

            case WizardPage.AdjustBase:
                currentPage = WizardPage.MoveSprite;
                break;

            case WizardPage.MoveSprite:
                if (moveSprite == null)
                {
                    EditorUtility.DisplayDialog("Error", "Please assign a move sprite.", "OK");
                    return;
                }
                ApplyMoveSprite();
                currentPage = WizardPage.WindupQuestion;
                break;

            case WizardPage.WindupQuestion:
                // Apply windup sprite (or copy idle if none provided)
                if (windupSprite != null)
                {
                    ApplyWindupSprite();
                }
                else
                {
                    CreateWindupFromIdle();
                }

                // Go to adjust page if different hitbox requested
                if (windupSprite != null && windupDifferentHitbox)
                {
                    currentPage = WizardPage.AdjustWindup;
                }
                else
                {
                    currentPage = WizardPage.AttackSprite;
                }
                break;

            case WizardPage.WindupSprite:
                // No longer used - skip to attack sprite
                currentPage = WizardPage.AttackSprite;
                break;

            case WizardPage.AdjustWindup:
                currentPage = WizardPage.AttackSprite;
                break;

            case WizardPage.AttackSprite:
                if (attackSprite == null)
                {
                    EditorUtility.DisplayDialog("Error", "Please assign an attack sprite.", "OK");
                    return;
                }
                ApplyAttackSprite();
                if (attackDifferentHitbox)
                {
                    currentPage = WizardPage.AdjustAttack;
                }
                else
                {
                    currentPage = WizardPage.EnemySize;
                }
                break;

            case WizardPage.AdjustAttack:
                currentPage = WizardPage.EnemySize;
                break;

            case WizardPage.EnemySize:
                ApplyEnemySize();
                currentPage = WizardPage.AttackType;
                break;

            case WizardPage.AttackType:
                CreateAttack();
                ApplyAttackTypeSpecific(); // Apply attack type specific settings immediately
                currentPage = WizardPage.BasicStats;
                break;

            case WizardPage.BasicStats:
                ApplyBasicStats();
                currentPage = WizardPage.AttackTiming;
                break;

            case WizardPage.AttackTiming:
                ApplyAttackTiming();
                currentPage = WizardPage.StraferSettings;
                break;

            case WizardPage.StraferSettings:
                ApplyStraferSettings();
                currentPage = WizardPage.SoundEffects;
                break;

            case WizardPage.SoundEffects:
                SaveSoundEffects();
                currentPage = WizardPage.Drops;
                break;

            case WizardPage.Drops:
                // Check if creating new material drop
                if (!hasDrops && !string.IsNullOrEmpty(dropMaterialName) && dropTexture != null)
                {
                    CreateDropMaterial();
                }
                currentPage = WizardPage.Finalize;
                break;

            case WizardPage.Finalize:
                FinalizeEnemy();
                // Don't close - enemy is still selected in scene
                // User can continue editing or close wizard manually
                EditorUtility.DisplayDialog("Wizard Complete!",
                    $"Enemy '{enemyName}' has been created successfully!\n\n" +
                    "The enemy GameObject is selected in the scene and has been saved as a prefab.\n\n" +
                    "You can now:\n" +
                    "- Further adjust the enemy in the scene\n" +
                    "- Test the enemy\n" +
                    "- Find the prefab at: Assets/Prefabs/Char/" + enemyName + "/",
                    "OK");
                break;
        }

        Repaint();
    }

    void OnBackButton()
    {
        // Back button with conditional logic
        switch (currentPage)
        {
            case WizardPage.BaseSprite:
                currentPage = WizardPage.Name;
                break;
            case WizardPage.AdjustBase:
                currentPage = WizardPage.BaseSprite;
                break;
            case WizardPage.MoveSprite:
                currentPage = WizardPage.AdjustBase;
                break;
            case WizardPage.WindupQuestion:
                currentPage = WizardPage.MoveSprite;
                break;
            case WizardPage.WindupSprite:
                currentPage = WizardPage.WindupQuestion;
                break;
            case WizardPage.AdjustWindup:
                currentPage = WizardPage.WindupQuestion;
                break;
            case WizardPage.AttackSprite:
                // Go back based on whether windup had different hitbox
                if (windupSprite != null && windupDifferentHitbox)
                {
                    currentPage = WizardPage.AdjustWindup;
                }
                else
                {
                    currentPage = WizardPage.WindupQuestion;
                }
                break;
            case WizardPage.AdjustAttack:
                currentPage = WizardPage.AttackSprite;
                break;
            case WizardPage.EnemySize:
                // Go back based on whether attack had different hitbox
                if (attackDifferentHitbox)
                {
                    currentPage = WizardPage.AdjustAttack;
                }
                else
                {
                    currentPage = WizardPage.AttackSprite;
                }
                break;
            case WizardPage.AttackType:
                currentPage = WizardPage.EnemySize;
                break;
            case WizardPage.BasicStats:
                currentPage = WizardPage.AttackType;
                break;
            case WizardPage.AttackTiming:
                currentPage = WizardPage.BasicStats;
                break;
            case WizardPage.StraferSettings:
                currentPage = WizardPage.AttackTiming;
                break;
            case WizardPage.SoundEffects:
                currentPage = WizardPage.StraferSettings;
                break;
            case WizardPage.Drops:
                currentPage = WizardPage.SoundEffects;
                break;
            case WizardPage.Finalize:
                currentPage = WizardPage.Drops;
                break;
        }

        Repaint();
    }

    void CreateInitialSetup()
    {
        Debug.Log("[CreateInitialSetup] Starting");

        // Create folder structure
        string enemyFolder = "Assets/Prefabs/Char/" + enemyName;
        string materialsFolder = enemyFolder + "/Materials";

        if (!AssetDatabase.IsValidFolder(enemyFolder))
        {
            Debug.Log($"[CreateInitialSetup] Creating folder: {enemyFolder}");
            string[] folders = enemyFolder.Split('/');
            string currentPath = folders[0];
            for (int i = 1; i < folders.Length; i++)
            {
                string nextPath = currentPath + "/" + folders[i];
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, folders[i]);
                }
                currentPath = nextPath;
            }
        }

        if (!AssetDatabase.IsValidFolder(materialsFolder))
        {
            Debug.Log($"[CreateInitialSetup] Creating materials folder: {materialsFolder}");
            AssetDatabase.CreateFolder(enemyFolder, "Materials");
        }

        // Create empty folders for textures, SFX, and misc
        string texturesFolder = enemyFolder + "/Textures";
        if (!AssetDatabase.IsValidFolder(texturesFolder))
        {
            AssetDatabase.CreateFolder(enemyFolder, "Textures");
        }

        string sfxFolder = enemyFolder + "/SFX";
        if (!AssetDatabase.IsValidFolder(sfxFolder))
        {
            AssetDatabase.CreateFolder(enemyFolder, "SFX");
        }

        string miscFolder = enemyFolder + "/Misc";
        if (!AssetDatabase.IsValidFolder(miscFolder))
        {
            AssetDatabase.CreateFolder(enemyFolder, "Misc");
        }

        Debug.Log("[CreateInitialSetup] Creating GameObject hierarchy");
        // Create GameObject hierarchy
        GameObject tempObject = new GameObject(enemyName);

        // Add components
        tempObject.AddComponent<NavMeshAgent>();
        Enemy enemyScript = tempObject.AddComponent<Enemy>();
        enemyScript.enemyName = enemyName;
        tempObject.AddComponent<Strafer>();

        Animator animator = tempObject.AddComponent<Animator>();
        RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Prefabs/EnemyAnimator.controller");
        if (controller != null) animator.runtimeAnimatorController = controller;

        // Create body
        bodyObject = new GameObject(enemyName + "Body");
        bodyObject.transform.SetParent(tempObject.transform);
        bodyObject.transform.localPosition = new Vector3(0, 0.75f, 0);
        bodyObject.AddComponent<Billboard>();

        // Create idle child (plane)
        idleChild = GameObject.CreatePrimitive(PrimitiveType.Plane);
        idleChild.name = enemyName + "Idle";
        idleChild.transform.SetParent(bodyObject.transform);
        idleChild.transform.localPosition = Vector3.zero;
        idleChild.transform.localRotation = Quaternion.Euler(90, 0, 0);
        idleChild.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);

        // Remove mesh collider, add capsule collider (Z-axis aligned)
        // Scale collider to match the 0.15 scale of parent (10 units in local space = 1.5 units in world space)
        DestroyImmediate(idleChild.GetComponent<MeshCollider>());
        CapsuleCollider capsule = idleChild.AddComponent<CapsuleCollider>();
        capsule.direction = 2; // Z-axis
        capsule.radius = 5f;  // 5 * 0.15 = 0.75 world units
        capsule.height = 10f; // 10 * 0.15 = 1.5 world units

        // Create GlowPart
        glowPart = GameObject.CreatePrimitive(PrimitiveType.Plane);
        glowPart.name = "GlowPart";
        glowPart.transform.SetParent(idleChild.transform);
        glowPart.transform.localPosition = new Vector3(0, 0.3f, 0);
        glowPart.transform.localRotation = Quaternion.identity;
        glowPart.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
        glowPart.layer = 6; // Color layer
        DestroyImmediate(glowPart.GetComponent<MeshCollider>());

        Debug.Log("[CreateInitialSetup] Creating material assets");
        // Create material assets
        idleMaterial = CreateTransparentMaterial(materialsFolder + "/" + enemyName + "Idle.mat");
        glowMaterial = CreateEmissiveMaterial(materialsFolder + "/" + enemyName + "Glow.mat");
        moveMaterial = CreateTransparentMaterial(materialsFolder + "/" + enemyName + "Move.mat");

        // Assign materials
        idleChild.GetComponent<MeshRenderer>().material = idleMaterial;
        glowPart.GetComponent<MeshRenderer>().material = glowMaterial;

        // Set tags
        tempObject.tag = "Enemy";
        bodyObject.tag = "Enemy";
        idleChild.tag = "Enemy";
        glowPart.tag = "Enemy";

        Debug.Log("[CreateInitialSetup] Saving prefab");
        // Save as prefab, but keep scene instance for editing
        string prefabPath = enemyFolder + "/" + enemyName + ".prefab";
        PrefabUtility.SaveAsPrefabAsset(tempObject, prefabPath);

        Debug.Log("[CreateInitialSetup] Keeping scene instance for live editing");
        // Keep the scene instance (tempObject) for editing - we'll save to prefab after each change
        // Don't destroy or open in isolation - work with scene instance instead

        // Store references to the scene instance GameObjects
        enemyPrefab = tempObject;
        bodyObject = tempObject.transform.Find(enemyName + "Body").gameObject;
        idleChild = bodyObject.transform.Find(enemyName + "Idle").gameObject;
        glowPart = idleChild.transform.Find("GlowPart").gameObject;

        // Select the object in the scene
        Selection.activeGameObject = tempObject;
        SceneView.FrameLastActiveSceneView();

        Debug.Log("[CreateInitialSetup] Complete");
    }

    void ApplyBaseSprites()
    {
        // Convert textures to sprite format
        ConvertToSprite(baseSprite);
        ConvertToSprite(glowSprite);
        if (emissionMap != null) ConvertToSprite(emissionMap);

        // Apply textures to materials
        idleMaterial.mainTexture = baseSprite;
        glowMaterial.mainTexture = glowSprite;
        glowMaterial.SetColor("_EmissionColor", glowColor * 2f);

        // Apply emission map if provided
        if (emissionMap != null)
        {
            glowMaterial.SetTexture("_EmissionMap", emissionMap);
        }

        EditorUtility.SetDirty(idleMaterial);
        EditorUtility.SetDirty(glowMaterial);
        AssetDatabase.SaveAssets();
        SavePrefabChanges();
    }

    void ApplyMoveSprite()
    {
        ConvertToSprite(moveSprite);
        moveMaterial.mainTexture = moveSprite;

        if (moveDifferentHitbox)
        {
            // Create move child by duplicating idle structure
            moveChild = GameObject.CreatePrimitive(PrimitiveType.Plane);
            moveChild.name = enemyName + "Move";
            moveChild.transform.SetParent(bodyObject.transform);
            moveChild.transform.localPosition = idleChild.transform.localPosition;
            moveChild.transform.localRotation = idleChild.transform.localRotation;
            moveChild.transform.localScale = idleChild.transform.localScale;

            // Copy collider settings
            DestroyImmediate(moveChild.GetComponent<MeshCollider>());
            CapsuleCollider idleCapsule = idleChild.GetComponent<CapsuleCollider>();
            CapsuleCollider moveCapsule = moveChild.AddComponent<CapsuleCollider>();
            moveCapsule.direction = idleCapsule.direction;
            moveCapsule.radius = idleCapsule.radius;
            moveCapsule.height = idleCapsule.height;

            moveChild.GetComponent<MeshRenderer>().material = moveMaterial;
            moveChild.tag = "Enemy";

            // Create GlowPart for move
            GameObject moveGlow = GameObject.CreatePrimitive(PrimitiveType.Plane);
            moveGlow.name = "GlowPart";
            moveGlow.transform.SetParent(moveChild.transform);
            moveGlow.transform.localPosition = glowPart.transform.localPosition;
            moveGlow.transform.localRotation = glowPart.transform.localRotation;
            moveGlow.transform.localScale = glowPart.transform.localScale;
            moveGlow.layer = 6;
            moveGlow.GetComponent<MeshRenderer>().material = glowMaterial;
            moveGlow.tag = "Enemy";
            DestroyImmediate(moveGlow.GetComponent<MeshCollider>());
        }
        else
        {
            // No separate move child - move will use idle child with material swap
            moveChild = null; // Don't create separate child, just use material
        }

        EditorUtility.SetDirty(moveMaterial);
        AssetDatabase.SaveAssets();
        SavePrefabChanges();
    }

    void CreateWindupFromIdle()
    {
        string matPath = "Assets/Prefabs/Char/" + enemyName + "/Materials/";
        windupMaterial = CreateTransparentMaterial(matPath + enemyName + "Windup.mat");
        windupMaterial.mainTexture = idleMaterial.mainTexture;
        EditorUtility.SetDirty(windupMaterial);
        AssetDatabase.SaveAssets();
        SavePrefabChanges();
    }

    void ApplyWindupSprite()
    {
        ConvertToSprite(windupSprite);

        string matPath = "Assets/Prefabs/Char/" + enemyName + "/Materials/";
        windupMaterial = CreateTransparentMaterial(matPath + enemyName + "Windup.mat");
        windupMaterial.mainTexture = windupSprite;

        if (windupDifferentHitbox)
        {
            // Create windup child by duplicating idle structure
            windupChild = GameObject.CreatePrimitive(PrimitiveType.Plane);
            windupChild.name = enemyName + "Windup";
            windupChild.transform.SetParent(bodyObject.transform);
            windupChild.transform.localPosition = idleChild.transform.localPosition;
            windupChild.transform.localRotation = idleChild.transform.localRotation;
            windupChild.transform.localScale = idleChild.transform.localScale;

            // Copy collider settings
            DestroyImmediate(windupChild.GetComponent<MeshCollider>());
            CapsuleCollider idleCapsule = idleChild.GetComponent<CapsuleCollider>();
            CapsuleCollider windupCapsule = windupChild.AddComponent<CapsuleCollider>();
            windupCapsule.direction = idleCapsule.direction;
            windupCapsule.radius = idleCapsule.radius;
            windupCapsule.height = idleCapsule.height;

            windupChild.GetComponent<MeshRenderer>().material = windupMaterial;
            windupChild.tag = "Enemy";

            // Create GlowPart for windup
            GameObject windupGlow = GameObject.CreatePrimitive(PrimitiveType.Plane);
            windupGlow.name = "GlowPart";
            windupGlow.transform.SetParent(windupChild.transform);
            windupGlow.transform.localPosition = glowPart.transform.localPosition;
            windupGlow.transform.localRotation = glowPart.transform.localRotation;
            windupGlow.transform.localScale = glowPart.transform.localScale;
            windupGlow.layer = 6;
            windupGlow.GetComponent<MeshRenderer>().material = glowMaterial;
            windupGlow.tag = "Enemy";
            DestroyImmediate(windupGlow.GetComponent<MeshCollider>());
        }

        EditorUtility.SetDirty(windupMaterial);
        AssetDatabase.SaveAssets();
        SavePrefabChanges();
    }

    void ApplyAttackSprite()
    {
        ConvertToSprite(attackSprite);

        string matPath = "Assets/Prefabs/Char/" + enemyName + "/Materials/";
        attackMaterial = CreateTransparentMaterial(matPath + enemyName + "Attack.mat");
        attackMaterial.mainTexture = attackSprite;

        if (attackDifferentHitbox)
        {
            // Create attack child by duplicating idle structure (without GlowPart)
            attackChild = GameObject.CreatePrimitive(PrimitiveType.Plane);
            attackChild.name = enemyName + "Attack";
            attackChild.transform.SetParent(bodyObject.transform);
            attackChild.transform.localPosition = idleChild.transform.localPosition;
            attackChild.transform.localRotation = idleChild.transform.localRotation;
            attackChild.transform.localScale = idleChild.transform.localScale;

            // Remove mesh collider, add capsule collider
            DestroyImmediate(attackChild.GetComponent<MeshCollider>());
            CapsuleCollider idleCapsule = idleChild.GetComponent<CapsuleCollider>();
            CapsuleCollider attackCapsule = attackChild.AddComponent<CapsuleCollider>();
            attackCapsule.direction = idleCapsule.direction;
            attackCapsule.radius = idleCapsule.radius;
            attackCapsule.height = idleCapsule.height;

            // Assign material
            attackChild.GetComponent<MeshRenderer>().material = attackMaterial;
            attackChild.tag = "Enemy";

            // Note: Attack child doesn't get a GlowPart
        }
        else
        {
            // No separate attack child - attack will use idle child with material swap
            attackChild = idleChild;
        }

        EditorUtility.SetDirty(attackMaterial);
        AssetDatabase.SaveAssets();
        SavePrefabChanges();
    }

    void ReuseAttackSpriteForWindup()
    {
        if (windupMaterial != null)
        {
            windupMaterial.mainTexture = attackSprite;
            EditorUtility.SetDirty(windupMaterial);
            AssetDatabase.SaveAssets();
            SavePrefabChanges();
        }
    }

    void ApplyEnemySize()
    {
        bodyObject.transform.localScale = Vector3.one * enemySize;
        SavePrefabChanges();
    }

    void CreateAttack()
    {
        Enemy enemyScript = enemyPrefab.GetComponent<Enemy>();
        if (enemyScript != null)
        {
            enemyScript.attacks = new EnemyAttack[1];
            enemyScript.attacks[0] = new EnemyAttack();
            enemyScript.attacks[0].attackType = attackType;
        }
        SavePrefabChanges();
    }

    void ApplyBasicStats()
    {
        Enemy enemyScript = enemyPrefab.GetComponent<Enemy>();
        if (enemyScript != null)
        {
            enemyScript.maxHP = maxHP;
            enemyScript.moveSpeed = moveSpeed;
            enemyScript.damage = damage;
            enemyScript.range = range;
            enemyScript.moveAnimRate = moveAnimRate;
        }
        SavePrefabChanges();
    }

    void ApplyAttackTiming()
    {
        Enemy enemyScript = enemyPrefab.GetComponent<Enemy>();
        if (enemyScript != null)
        {
            enemyScript.delay = delay;
            enemyScript.duration = duration;
            enemyScript.cooldown = cooldown;
            enemyScript.moveAttack = moveAttack;
        }
        SavePrefabChanges();
    }

    void ApplyStraferSettings()
    {
        Strafer straferScript = enemyPrefab.GetComponent<Strafer>();
        if (straferScript != null)
        {
            straferScript.isStrafe = isStrafe;
            straferScript.strafeDistance = strafeDistance;
            straferScript.isSneaky = isSneaky;
            straferScript.sneakAngle = sneakAngle;
        }
        SavePrefabChanges();
    }

    void ApplyAttackTypeSpecific()
    {
        Enemy enemyScript = enemyPrefab.GetComponent<Enemy>();

        switch (attackType)
        {
            case EnemyAttack.AttackType.Melee:
                CreateMeleeHitbox();
                break;
            case EnemyAttack.AttackType.Projectile:
                CreateProjectile();
                break;
            case EnemyAttack.AttackType.Lunging:
                CreateLungingHitbox();
                if (enemyScript != null)
                {
                    if (lungeJumps) enemyScript.jumpHeight = jumpHeight;
                    enemyScript.lungeDistance = lungeDistance;
                    enemyScript.bounceBack = bounceBack;
                }
                break;
        }
        SavePrefabChanges();
    }

    void CreateMeleeHitbox()
    {
        if (attackChild == null)
        {
            Debug.LogWarning("Attack child not found, cannot create hitbox.");
            return;
        }

        if (meleeSpecificHitbox)
        {
            // Create separate hitbox child
            GameObject hitbox = new GameObject("Hitbox");
            hitbox.transform.SetParent(attackChild.transform);
            hitbox.transform.localPosition = Vector3.zero;
            BoxCollider col = hitbox.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = Vector3.one;
            hitbox.AddComponent<AttackHitbox>();
            hitbox.tag = "Enemy";

            EditorGUILayout.HelpBox("Adjust the hitbox collider in the scene.", MessageType.Info);
            SelectAndFrameObject(hitbox);
        }
        else
        {
            // Use attack child's existing capsule collider as trigger
            if (attackChild.TryGetComponent<CapsuleCollider>(out var capsule))
            {
                capsule.isTrigger = true;
            }
            attackChild.AddComponent<AttackHitbox>();
        }
    }

    void CreateLungingHitbox()
    {
        if (attackChild == null)
        {
            Debug.LogWarning("Attack child not found, cannot create hitbox.");
            return;
        }

        GameObject hitbox;
        if (lungeSpecificHitbox)
        {
            // Create separate hitbox child
            hitbox = new GameObject("Hitbox");
            hitbox.transform.SetParent(attackChild.transform);
            hitbox.transform.localPosition = Vector3.zero;
            BoxCollider col = hitbox.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = Vector3.one;
            hitbox.AddComponent<AttackHitbox>();
            hitbox.tag = "Enemy";

            EditorGUILayout.HelpBox("Adjust the hitbox collider in the scene.", MessageType.Info);
            SelectAndFrameObject(hitbox);
        }
        else
        {
            // Use attack child's existing capsule collider as trigger
            if (attackChild.TryGetComponent<CapsuleCollider>(out var capsule))
            {
                capsule.isTrigger = true;
            }
            attackChild.AddComponent<AttackHitbox>();
        }

        // Add rigidbody only if jumping and attack child is separate from idle
        if (lungeJumps && attackChild != null && attackChild != idleChild)
        {
            Rigidbody rb = attackChild.AddComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }
    }

    void CreateProjectile()
    {
        string basePath = "Assets/Prefabs/Char/" + enemyName;

        // Create projectile prefab
        GameObject projectilePrefab = new GameObject(enemyName + "Projectile");
        projectilePrefab.layer = 6; // Color layer

        EnemyProjectile projScript = projectilePrefab.AddComponent<EnemyProjectile>();
        projScript.projectileSpeed = projectileSpeed;
        projScript.travelArc = usingGravity;
        projScript.arcHeight = arcHeight;
        projScript.dropOff = dropOff;
        projScript.damage = damage;
        projScript.isAOE = isAOE;
        projScript.splashRadius = splashRadius;
        if (hasProjectileHitSound && projectileHitSFX != null)
        {
            projScript.hitClip = projectileHitSFX;
            projScript.hitClipVolume = 1f;
        }

        // Add sprite if specified
        if (projectileHasSprite && projectileSprite != null)
        {
            // Convert texture to sprite
            ConvertToSprite(projectileSprite);

            // Create Sprite child object
            GameObject spriteObj = GameObject.CreatePrimitive(PrimitiveType.Plane);
            spriteObj.name = "Sprite";
            spriteObj.transform.SetParent(projectilePrefab.transform);
            spriteObj.transform.localPosition = Vector3.zero;
            spriteObj.transform.localRotation = Quaternion.Euler(90, 0, 0);
            spriteObj.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);

            // Remove the plane's collider
            DestroyImmediate(spriteObj.GetComponent<MeshCollider>());

            // Create or reuse material
            string spriteMaterialPath = basePath + "/Materials/" + enemyName + "ProjectileSprite.mat";
            Material spriteMat;

            // Check if a material with the same texture already exists
            Material existingMat = null;
            string[] allMaterials = AssetDatabase.FindAssets("t:Material", new[] { basePath + "/Materials" });
            foreach (string guid in allMaterials)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat != null && mat.GetTexture("_BaseMap") == projectileSprite)
                {
                    existingMat = mat;
                    break;
                }
            }

            if (existingMat != null)
            {
                spriteMat = existingMat;
            }
            else
            {
                spriteMat = CreateTransparentMaterial(spriteMaterialPath);
                spriteMat.SetTexture("_BaseMap", projectileSprite);
                AssetDatabase.SaveAssets();
            }

            MeshRenderer spriteRenderer = spriteObj.GetComponent<MeshRenderer>();
            spriteRenderer.material = spriteMat;
        }
        // Add mesh if specified (only if not using sprite)
        else if (projectileHasMesh)
        {
            MeshFilter mf = projectilePrefab.AddComponent<MeshFilter>();
            if (projectileMesh != null)
                mf.mesh = projectileMesh;
            else
                mf.mesh = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");

            MeshRenderer mr = projectilePrefab.AddComponent<MeshRenderer>();
            Material projMat = CreateEmissiveMaterial(basePath + "/Materials/" + enemyName + "Projectile.mat");
            projMat.SetColor("_EmissionColor", glowColor * 1f);
            mr.material = projMat;

            if (projectileMesh == null)
                projectilePrefab.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        }

        // Add particles
        if (hasParticles)
        {
            ParticleSystem ps = projectilePrefab.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            // Auto-assignment happens in EnemyProjectile.Start(), no need to set here
        }

        if (hasHitParticles)
        {
            GameObject hitObj = new GameObject("Hit");
            hitObj.transform.SetParent(projectilePrefab.transform);
            hitObj.layer = 6;
            ParticleSystem hitPs = hitObj.AddComponent<ParticleSystem>();
            var hitMain = hitPs.main;
            hitMain.loop = false;
            hitMain.duration = 0.5f;
            // Auto-assignment happens in EnemyProjectile.Start(), no need to set here
        }

        // Add rigidbody (all projectiles use physics now)
        Rigidbody rb = projectilePrefab.AddComponent<Rigidbody>();
        rb.useGravity = false; // Gravity controlled manually in script
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        // Add collider
        SphereCollider sphere = projectilePrefab.AddComponent<SphereCollider>();
        sphere.isTrigger = true;

        // Save projectile prefab
        string prefabPath = basePath + "/" + enemyName + "Projectile.prefab";
        PrefabUtility.SaveAsPrefabAsset(projectilePrefab, prefabPath);
        DestroyImmediate(projectilePrefab);

        // Assign projectile prefab to Enemy component using SerializedObject
        Enemy enemyScript = enemyPrefab.GetComponent<Enemy>();
        if (enemyScript != null)
        {
            GameObject savedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            SerializedObject serializedEnemy = new SerializedObject(enemyScript);
            SerializedProperty projectileProp = serializedEnemy.FindProperty("projectilePrefab");
            projectileProp.objectReferenceValue = savedPrefab;
            serializedEnemy.ApplyModifiedProperties();
            SavePrefabChanges();
        }

        // Create ProjectileOrigin
        if (attackChild != null)
        {
            GameObject origin = new GameObject("ProjectileOrigin");
            origin.transform.SetParent(attackChild.transform);
            origin.transform.localPosition = new Vector3(0, 0, 1);

            // Create marker cube
            projectileOriginMarker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            projectileOriginMarker.transform.SetParent(origin.transform);
            projectileOriginMarker.transform.localPosition = Vector3.zero;
            projectileOriginMarker.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
            MeshRenderer markerRenderer = projectileOriginMarker.GetComponent<MeshRenderer>();
            Material markerMat = new Material(Shader.Find("Standard"));
            markerMat.color = Color.red;
            markerRenderer.material = markerMat;
            DestroyImmediate(projectileOriginMarker.GetComponent<BoxCollider>());

            EditorGUILayout.HelpBox("Adjust the ProjectileOrigin position and projectile collider in the scene.", MessageType.Info);
            SelectAndFrameObject(origin);
        }

        // No need to apply projectile settings to enemy - they're on the prefab now
    }

    void CreateDropMaterial()
    {
        string enemyFolder = "Assets/Prefabs/Char/" + enemyName;
        string dropFolder = enemyFolder + "/Drop";

        // Create Drop subfolder
        if (!AssetDatabase.IsValidFolder(dropFolder))
        {
            AssetDatabase.CreateFolder(enemyFolder, "Drop");
        }

        // Convert texture to sprite with same settings as state sprites
        ConvertToSprite(dropTexture);
        Sprite iconSprite = null;
        if (dropIcon != null)
        {
            ConvertToSprite(dropIcon);
            string iconPath = AssetDatabase.GetAssetPath(dropIcon);
            iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
        }
        else
        {
            string texturePath = AssetDatabase.GetAssetPath(dropTexture);
            iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
        }

        // Create material from texture
        Material dropMat = CreateTransparentMaterial(dropFolder + "/" + dropMaterialName + "Pickup.mat");
        dropMat.SetTexture("_BaseMap", dropTexture);
        AssetDatabase.SaveAssets();

        // Create Item ScriptableObject
        Item itemAsset = ScriptableObject.CreateInstance<Item>();
        itemAsset.itemName = dropMaterialName;
        itemAsset.isMaterial = true;
        itemAsset.itemIcon = iconSprite;

        string itemPath = dropFolder + "/" + dropMaterialName + ".asset";
        AssetDatabase.CreateAsset(itemAsset, itemPath);
        AssetDatabase.SaveAssets();

        // Load the MyteEye prefab as template
        GameObject templatePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Char/Myte/Drop/MyteEye.prefab");
        if (templatePrefab == null)
        {
            Debug.LogError("MyteEye template prefab not found at Assets/Prefabs/Char/Myte/Drop/MyteEye.prefab");
            return;
        }

        // Instantiate a copy
        GameObject dropPrefab = PrefabUtility.InstantiatePrefab(templatePrefab) as GameObject;
        dropPrefab.name = dropMaterialName;

        // Update MaterialDrop component with new item
        MaterialDrop materialDropComponent = dropPrefab.GetComponent<MaterialDrop>();
        if (materialDropComponent != null)
        {
            materialDropComponent.item = itemAsset;
        }

        // Update PickupModel materials
        Transform pickupModel = dropPrefab.transform.Find("PickupModel");
        if (pickupModel != null)
        {
            Transform side1 = pickupModel.Find("Side1");
            Transform side2 = pickupModel.Find("Side2");

            if (side1 != null)
            {
                Renderer side1Renderer = side1.GetComponent<Renderer>();
                if (side1Renderer != null)
                {
                    side1Renderer.sharedMaterial = dropMat;
                }
            }

            if (side2 != null)
            {
                Renderer side2Renderer = side2.GetComponent<Renderer>();
                if (side2Renderer != null)
                {
                    side2Renderer.sharedMaterial = dropMat;
                }
            }
        }

        // Update Beam light color to enemy's signature color
        Transform beam = dropPrefab.transform.Find("Beam");
        if (beam != null)
        {
            Light beamLight = beam.GetComponent<Light>();
            if (beamLight != null)
            {
                beamLight.color = glowColor;
            }
        }

        // Save as new prefab in Drop folder
        string prefabPath = dropFolder + "/" + dropMaterialName + ".prefab";
        PrefabUtility.SaveAsPrefabAsset(dropPrefab, prefabPath);
        DestroyImmediate(dropPrefab);

        // Assign the created prefab to enemyDrop so it gets used
        enemyDrop = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        hasDrops = true;

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Created drop material: {dropMaterialName} at {dropFolder}");
    }

    void SaveSoundEffects()
    {
        string sfxPath = "Assets/Prefabs/Char/" + enemyName + "/SFX/";

        if (hasAgroSound && agroClip != null)
            SaveAudioClip(agroClip, sfxPath + enemyName + "Agro.wav");
        if (hasDieSound && dieClip != null)
            SaveAudioClip(dieClip, sfxPath + enemyName + "Die.wav");
        if (hasMoveSound && moveSFX != null)
            SaveAudioClip(moveSFX, sfxPath + enemyName + "Move.wav");
        if (hasWindupSound && windupClip != null)
            SaveAudioClip(windupClip, sfxPath + enemyName + "Windup.wav");
        if (hasAttackSound && attackClip != null)
            SaveAudioClip(attackClip, sfxPath + enemyName + "Attack.wav");
        if (hasDamagedSound && damagedSFX != null)
            SaveAudioClip(damagedSFX, sfxPath + enemyName + "Damaged.wav");
        if (hasAttackHitSound && attackHitSFX != null)
            SaveAudioClip(attackHitSFX, sfxPath + enemyName + "AttackHit.wav");
        if (hasProjectileHitSound && projectileHitSFX != null)
            SaveAudioClip(projectileHitSFX, sfxPath + enemyName + "ProjectileHit.wav");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    void FinalizeEnemy()
    {
        // Destroy projectile marker if exists
        if (projectileOriginMarker != null)
            DestroyImmediate(projectileOriginMarker);

        // Add AudioSource if not present
        if (enemyPrefab.GetComponent<AudioSource>() == null)
            enemyPrefab.AddComponent<AudioSource>();

        // Set NavMeshAgent properties based on collider
        NavMeshAgent agent = enemyPrefab.GetComponent<NavMeshAgent>();
        CapsuleCollider capsule = idleChild != null ? idleChild.GetComponent<CapsuleCollider>() : null;
        if (agent != null && capsule != null)
        {
            // Calculate world space collider size (local size * child scale * body scale)
            float worldRadius = capsule.radius * 0.15f * enemySize;
            float worldHeight = capsule.height * 0.15f * enemySize;

            agent.radius = worldRadius;
            agent.height = worldHeight * 1.2f;
            agent.obstacleAvoidanceType = UnityEngine.AI.ObstacleAvoidanceType.LowQualityObstacleAvoidance;
        }

        // Apply drops
        Enemy enemyScript = enemyPrefab.GetComponent<Enemy>();
        if (hasDrops && enemyDrop != null && enemyScript != null)
        {
            enemyScript.GetType().GetField("enemyDrop", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(enemyScript, enemyDrop);
        }

        // Add EnemyVariableSetter and run it
        EnemyVariableSetter setter = enemyPrefab.GetComponent<EnemyVariableSetter>();
        if (setter == null)
            setter = enemyPrefab.AddComponent<EnemyVariableSetter>();

        setter.CreateMissingComponents();
        setter.SetVariables();

        // NOW create all folders and save all assets
        string basePath = "Assets/Prefabs/Char/" + enemyName;
        if (!AssetDatabase.IsValidFolder(basePath))
            AssetDatabase.CreateFolder("Assets/Prefabs/Char", enemyName);
        if (!AssetDatabase.IsValidFolder(basePath + "/SFX"))
            AssetDatabase.CreateFolder(basePath, "SFX");
        if (!AssetDatabase.IsValidFolder(basePath + "/Textures"))
            AssetDatabase.CreateFolder(basePath, "Textures");
        if (!AssetDatabase.IsValidFolder(basePath + "/Materials"))
            AssetDatabase.CreateFolder(basePath, "Materials");
        if (!AssetDatabase.IsValidFolder(basePath + "/Misc"))
            AssetDatabase.CreateFolder(basePath, "Misc");

        // Save all runtime materials as assets
        string matPath = basePath + "/Materials/";
        if (idleMaterial != null && !AssetDatabase.Contains(idleMaterial))
            AssetDatabase.CreateAsset(idleMaterial, matPath + enemyName + "Idle.mat");
        if (glowMaterial != null && !AssetDatabase.Contains(glowMaterial))
            AssetDatabase.CreateAsset(glowMaterial, matPath + enemyName + "Glow.mat");
        if (moveMaterial != null && !AssetDatabase.Contains(moveMaterial))
            AssetDatabase.CreateAsset(moveMaterial, matPath + enemyName + "Move.mat");
        if (windupMaterial != null && !AssetDatabase.Contains(windupMaterial))
            AssetDatabase.CreateAsset(windupMaterial, matPath + enemyName + "Windup.mat");
        if (attackMaterial != null && !AssetDatabase.Contains(attackMaterial))
            AssetDatabase.CreateAsset(attackMaterial, matPath + enemyName + "Attack.mat");

        // Save the scene object as prefab, keep scene instance
        string prefabPath = basePath + "/" + enemyName + ".prefab";
        PrefabUtility.SaveAsPrefabAsset(enemyPrefab, prefabPath);

        // Keep the scene object (don't destroy it)
        Selection.activeGameObject = enemyPrefab;

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Success", $"Enemy '{enemyName}' created successfully at:\n{prefabPath}\n\nThe enemy GameObject remains in the scene for further editing.", "OK");
    }

    // Helper methods
    Material CreateTransparentMaterial(string path)
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            Debug.LogWarning("URP/Lit shader not found, falling back to Standard");
            urpLit = Shader.Find("Standard");
        }

        Material mat = new Material(urpLit);

        // Set to transparent mode for URP
        mat.SetFloat("_Surface", 1); // Transparent
        mat.SetFloat("_Blend", 0); // Alpha blend
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON"); // Disable preserve specular lighting
        mat.renderQueue = 3000;
        mat.SetFloat("_Smoothness", 0);
        mat.EnableKeyword("_SPECULARHIGHLIGHTS_OFF"); // Disable specular highlights
        mat.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF"); // Disable environment reflections

        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    Material CreateEmissiveMaterial(string path)
    {
        Material mat = CreateTransparentMaterial(path);
        mat.EnableKeyword("_EMISSION");
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        mat.renderQueue = 2999; // Render glow before base sprite to prevent see-through at distance
        return mat;
    }

    void SavePrefabChanges()
    {
        if (enemyPrefab != null)
        {
            string prefabPath = "Assets/Prefabs/Char/" + enemyName + "/" + enemyName + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(enemyPrefab, prefabPath);
            AssetDatabase.SaveAssets();
        }
    }

    Material CreateRuntimeTransparentMaterial()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            Debug.LogWarning("URP/Lit shader not found, falling back to Standard");
            urpLit = Shader.Find("Standard");
        }

        Material mat = new Material(urpLit);

        // Set to transparent mode for URP
        mat.SetFloat("_Surface", 1); // Transparent
        mat.SetFloat("_Blend", 0); // Alpha blend
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON"); // Disable preserve specular lighting
        mat.renderQueue = 3000;
        mat.SetFloat("_Smoothness", 0);
        mat.EnableKeyword("_SPECULARHIGHLIGHTS_OFF"); // Disable specular highlights
        mat.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF"); // Disable environment reflections

        // Don't create asset yet
        return mat;
    }

    Material CreateRuntimeEmissiveMaterial()
    {
        Material mat = CreateRuntimeTransparentMaterial();
        mat.EnableKeyword("_EMISSION");
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        return mat;
    }

    void ConvertToSprite(Texture2D texture)
    {
        if (texture == null) return;

        string path = AssetDatabase.GetAssetPath(texture);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.SaveAndReimport();
        }
    }

    void SaveAudioClip(AudioClip clip, string path)
    {
        string sourcePath = AssetDatabase.GetAssetPath(clip);
        if (!string.IsNullOrEmpty(sourcePath))
        {
            AssetDatabase.CopyAsset(sourcePath, path);
        }
    }

    void SelectAndFrameObject(GameObject obj)
    {
        if (obj == null) return;

        Selection.activeGameObject = obj;
        EditorGUIUtility.PingObject(obj);
        SceneView.FrameLastActiveSceneView();
    }

    void DrawTexturePreview(Texture2D texture, int maxWidth, int maxHeight)
    {
        if (texture == null) return;

        // Calculate aspect ratio and scale to fit
        float aspectRatio = (float)texture.width / texture.height;
        float displayWidth = maxWidth;
        float displayHeight = maxHeight;

        if (aspectRatio > 1) // Landscape
        {
            displayHeight = maxWidth / aspectRatio;
            if (displayHeight > maxHeight)
            {
                displayHeight = maxHeight;
                displayWidth = maxHeight * aspectRatio;
            }
        }
        else // Portrait or square
        {
            displayWidth = maxHeight * aspectRatio;
            if (displayWidth > maxWidth)
            {
                displayWidth = maxWidth;
                displayHeight = maxWidth / aspectRatio;
            }
        }

        // Center the preview
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        Rect rect = GUILayoutUtility.GetRect(displayWidth, displayHeight);

        // Draw checkerboard background for alpha transparency
        DrawCheckerboard(rect);
        // Draw texture with alpha
        GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit, true);

        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }

    void DrawSideBySidePreview(Texture2D texture1, Texture2D texture2, string label1, string label2)
    {
        if (texture1 == null && texture2 == null) return;

        EditorGUILayout.BeginHorizontal();

        // Left side - Idle sprite
        EditorGUILayout.BeginVertical(GUILayout.Width(160));
        EditorGUILayout.LabelField(label1, EditorStyles.boldLabel);
        if (texture1 != null)
        {
            Rect rect1 = GUILayoutUtility.GetRect(150, 200);
            DrawCheckerboard(rect1);
            // ScaleToFit maintains aspect ratio
            GUI.DrawTexture(rect1, texture1, ScaleMode.ScaleToFit, true);
        }
        EditorGUILayout.EndVertical();

        GUILayout.Space(20);

        // Right side - Comparison sprite
        EditorGUILayout.BeginVertical(GUILayout.Width(160));
        EditorGUILayout.LabelField(label2, EditorStyles.boldLabel);
        if (texture2 != null)
        {
            Rect rect2 = GUILayoutUtility.GetRect(150, 200);
            DrawCheckerboard(rect2);
            // ScaleToFit maintains aspect ratio
            GUI.DrawTexture(rect2, texture2, ScaleMode.ScaleToFit, true);
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    void DrawCheckerboard(Rect rect)
    {
        // Draw checkerboard pattern for alpha transparency visualization
        int checkerSize = 10;
        Color lightGray = new Color(0.8f, 0.8f, 0.8f);
        Color darkGray = new Color(0.6f, 0.6f, 0.6f);

        for (int y = 0; y < rect.height; y += checkerSize)
        {
            for (int x = 0; x < rect.width; x += checkerSize)
            {
                bool isLight = ((x / checkerSize) + (y / checkerSize)) % 2 == 0;
                EditorGUI.DrawRect(new Rect(rect.x + x, rect.y + y,
                    Mathf.Min(checkerSize, rect.width - x),
                    Mathf.Min(checkerSize, rect.height - y)),
                    isLight ? lightGray : darkGray);
            }
        }
    }
}
