using UnityEngine;

namespace ALMMD;

// Exact local transforms from the installed AL bone/000.unity3d prefab.
// cf_d/cf_dd are animation helpers; cf_s/cf_y and length nodes keep actor shape.
internal static class NativeRestPose
{
    public static bool TryRotation(string name, out Quaternion rotation) =>
        TryPose(name, out _, out rotation, out _);

    public static bool TryPose(string name, out Vector3 position, out Quaternion rotation, out Vector3 scale)
    {
        switch (name)
        {
            case "cf_d_ana":
                position = new Vector3(-0.0f, -1.0248451232910156f, -0.27237850427627563f);
                rotation = new Quaternion(3.617645205622466e-08f, -1.9744906865071243e-07f, -2.973201951306237e-08f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_arm00_L":
                position = new Vector3(-0.0f, -1.7763568394002505e-15f, 0.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_arm00_R":
                position = new Vector3(-0.0f, 0.0f, 1.734723475976807e-18f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_arm01_L":
                position = new Vector3(1.1102230246251565e-16f, 1.7763568394002505e-15f, 0.0f);
                rotation = new Quaternion(7.94122165696729e-23f, -0.008509714156389236f, 3.565015140338801e-05f, 0.9999638199806213f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_arm01_R":
                position = new Vector3(-0.0f, 1.7763568394002505e-15f, 0.0f);
                rotation = new Quaternion(1.0859320354938973e-05f, 0.008509714156389236f, -9.708798387464412e-08f, 0.9999638199806213f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_arm02_L":
                position = new Vector3(-1.0f, 8.377596532227471e-05f, 0.019999999552965164f);
                rotation = new Quaternion(7.94122165696729e-23f, -0.008509714156389236f, 3.565015140338801e-05f, 0.9999638199806213f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_arm02_R":
                position = new Vector3(1.0f, 1.7763568394002505e-15f, 0.019999999552965164f);
                rotation = new Quaternion(6.787061465729494e-06f, 0.008509714156389236f, -6.243296724051106e-08f, 0.9999638199806213f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_arm03_L":
                position = new Vector3(-1.899999976158142f, 0.0001531886518932879f, 0.029999999329447746f);
                rotation = new Quaternion(7.94122165696729e-23f, -0.008509714156389236f, 3.565015140338801e-05f, 0.9999638199806213f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_arm03_R":
                position = new Vector3(1.899999976158142f, 1.7763568394002505e-15f, 0.029999999329447746f);
                rotation = new Quaternion(-4.5067724840919254e-07f, 0.008509714156389236f, -3.8352734321733806e-09f, 0.9999638199806213f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_armcloth_B_L":
                position = new Vector3(-1.0f, 7.260709935508203e-06f, -0.31260088086128235f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_armcloth_B_R":
                position = new Vector3(1.0f, 0.0f, -0.3125997483730316f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_armcloth_F_L":
                position = new Vector3(-2.220446049250313e-16f, 7.260709935508203e-06f, 0.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_armcloth_F_R":
                position = new Vector3(-0.0f, 0.0f, 0.0f);
                rotation = new Quaternion(0.06819954514503479f, 0.010435571894049644f, 0.023378323763608932f, 0.9973431825637817f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_armsk_L_0_0":
                position = new Vector3(0.033339351415634155f, 1.521262191772621e-07f, 0.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_armsk_L_1_0":
                position = new Vector3(-0.3330000042915344f, 0.0f, 0.0f);
                rotation = new Quaternion(-4.237232687802884e-22f, 6.938893903907228e-18f, -2.4479376252741163e-23f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_armsk_L_2_0":
                position = new Vector3(-0.2150000035762787f, 0.0f, 0.0f);
                rotation = new Quaternion(0.00023593571677338332f, -0.03186729550361633f, -7.5403381742944475e-06f, 0.9994921088218689f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_armsk_R_0_0":
                position = new Vector3(-0.0f, 8.990678907139227e-05f, 0.0f);
                rotation = new Quaternion(7.38235394237563e-06f, -0.0797431543469429f, 3.234526593587361e-05f, 0.9968154430389404f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_armsk_R_1_0":
                position = new Vector3(0.3330000042915344f, 2.73776686299243e-06f, 0.0f);
                rotation = new Quaternion(1.392961075907806e-05f, 1.5407439555097887e-33f, 1.1051770392941918e-12f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_armsk_R_2_0":
                position = new Vector3(0.2145185023546219f, 9.684716815172578e-07f, 0.0f);
                rotation = new Quaternion(5.963001513009658e-06f, 0.03186440467834473f, 2.0459244751691585e-06f, 0.9994922280311584f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_backsk_C":
                position = new Vector3(1.0611161727069884e-08f, 0.0f, 0.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_backsk_L":
                position = new Vector3(-1.1799967288970947f, 2.3038994640955934e-06f, 0.0001656963722780347f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_backsk_L_00":
                position = new Vector3(-0.0f, 0.0f, 0.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_backsk_R":
                position = new Vector3(1.179998755455017f, 8.576452614761365e-07f, 0.00016116884944494814f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_backsk_R_00":
                position = new Vector3(-0.0f, 0.0f, 0.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_bust00":
                position = new Vector3(-0.0f, 0.10000000149011612f, 0.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_bust01_C":
                position = new Vector3(-0.0f, 0.0f, 0.14190132915973663f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_bust01an_C":
                position = new Vector3(-0.0f, 0.5998652577400208f, -0.2751358449459076f);
                rotation = new Quaternion(-0.32556816935539246f, 0.0f, -0.0f, 0.9455185532569885f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_bust01an_cm_C":
                position = new Vector3(-0.0f, 0.5998652577400208f, -0.25999999046325684f);
                rotation = new Quaternion(-0.32556816935539246f, 0.0f, -0.0f, 0.9455185532569885f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_bust02an_C":
                position = new Vector3(-0.0f, 0.0f, 0.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_bust02an_cm_C":
                position = new Vector3(-0.0f, 0.0f, 0.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_elbo_L":
                position = new Vector3(4.119211993725003e-08f, -8.837792482094416e-16f, -0.25f);
                rotation = new Quaternion(0.0f, -3.594690767272368e-08f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_elbo_R":
                position = new Vector3(-2.4781934371276293e-07f, 0.0f, -0.2499999701976776f);
                rotation = new Quaternion(0.0f, 2.1626317447953625e-07f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_elboback_L":
                position = new Vector3(-0.0f, 0.0f, 0.30000001192092896f);
                rotation = new Quaternion(-4.0389678347315804e-28f, 0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_elboback_R":
                position = new Vector3(-0.0f, 0.0f, 0.30000001192092896f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_forearm00_L":
                position = new Vector3(-0.0f, -1.7763568394002505e-15f, 0.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_forearm00_R":
                position = new Vector3(-0.0f, -4.839705385616697e-16f, 0.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_forearm01_L":
                position = new Vector3(4.440892098500626e-16f, -1.7763568394002505e-15f, 0.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_forearm01_R":
                position = new Vector3(4.440892098500626e-16f, 1.7763568394002505e-15f, 0.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_forearm02_L":
                position = new Vector3(-0.7900000214576721f, -1.7763568394002505e-15f, 0.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_forearm02_R":
                position = new Vector3(0.7900000214576721f, 3.552713678800501e-15f, 0.0f);
                rotation = new Quaternion(6.964806743781082e-06f, 0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0000025033950806f, 1.0000025033950806f); return true;
            case "cf_d_forearm03_L":
                position = new Vector3(-1.6130000352859497f, 1.7763568394002505e-15f, 8.35251048556529e-05f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_forearm03_R":
                position = new Vector3(1.6130000352859497f, 5.329070518200751e-15f, 0.0f);
                rotation = new Quaternion(1.114369024435291e-05f, 0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_forearm_wrist_L":
                position = new Vector3(-0.10000000149011612f, 0.0f, -5.551115123125783e-17f);
                rotation = new Quaternion(0.0f, 1.797346413923151e-07f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_forearm_wrist_R":
                position = new Vector3(0.10000000149011612f, 0.0f, 0.0f);
                rotation = new Quaternion(5.026305977332002e-13f, -5.591547278527287e-07f, 8.989114235191664e-07f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_hipleg1_L":
                position = new Vector3(-0.8127237558364868f, 0.3151889145374298f, 0.42890697717666626f);
                rotation = new Quaternion(0.034726954996585846f, 1.3560811886891407e-20f, -0.003924271557480097f, 0.9993891716003418f);
                scale = new Vector3(0.9626707434654236f, 1.0f, 0.8570031523704529f); return true;
            case "cf_d_hipleg1_R":
                position = new Vector3(0.8106889724731445f, 0.31518739461898804f, 0.428906112909317f);
                rotation = new Quaternion(0.034727465361356735f, -9.492579306816495e-20f, 0.004197952337563038f, 0.9993880391120911f);
                scale = new Vector3(0.9626713991165161f, 1.0f, 0.8570031523704529f); return true;
            case "cf_d_hipleg2_L":
                position = new Vector3(-0.39414089918136597f, -0.77260822057724f, 0.6086025238037109f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_hipleg2_R":
                position = new Vector3(0.3941427171230316f, -0.7726061940193176f, 0.6086004376411438f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_kneeB_L":
                position = new Vector3(-6.133611350378487e-06f, -7.013375125097809e-07f, -0.29999929666519165f);
                rotation = new Quaternion(-3.060162043766468e-07f, 0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_kneeB_R":
                position = new Vector3(-5.087304089101963e-07f, -6.960993346183386e-07f, -0.29999929666519165f);
                rotation = new Quaternion(-3.03730615769382e-07f, 0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_kneeF_L":
                position = new Vector3(-1.7781108908820897e-05f, 4.2080247908415913e-07f, 0.3220001459121704f);
                rotation = new Quaternion(-7.344389132413198e-07f, 0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_kneeF_R":
                position = new Vector3(-5.063981234343373e-07f, 4.176595780336356e-07f, 0.3220001459121704f);
                rotation = new Quaternion(-7.28953466477833e-07f, 0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_leg00_L":
                position = new Vector3(-0.0f, -4.400000095367432f, 0.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_leg00_R":
                position = new Vector3(-0.0f, -4.400000095367432f, 6.852157730108388e-17f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_leg01_L":
                position = new Vector3(-6.027271865605144e-06f, 4.239999771118164f, -0.2199999988079071f);
                rotation = new Quaternion(0.0f, 0.0f, -5.222584604780423e-07f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_leg01_R":
                position = new Vector3(-2.6073754710864705e-08f, 4.239999771118164f, -0.2199999988079071f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_leg02_L":
                position = new Vector3(-9.1581050583045e-06f, 2.5999999046325684f, -0.25f);
                rotation = new Quaternion(-0.04361938685178757f, -1.0756662760513791e-07f, -5.160703153705981e-07f, 0.9990482330322266f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_leg02_R":
                position = new Vector3(-1.4855856989015592e-07f, 2.6002390384674072f, -0.25f);
                rotation = new Quaternion(-0.04361938685178757f, 7.469456164699295e-08f, -8.987968413975977e-08f, 0.9990482330322266f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_leg03_L":
                position = new Vector3(-2.320100975339301e-06f, 0.8999999761581421f, -0.10000000149011612f);
                rotation = new Quaternion(-0.04361938685178757f, -2.898033244491671e-07f, 9.246210908031571e-08f, 0.9990482330322266f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_leg03_R":
                position = new Vector3(-6.397373226718628e-07f, 0.9002389311790466f, -0.10000000149011612f);
                rotation = new Quaternion(-0.04361938685178757f, 1.6143452796768543e-07f, -1.3708093149489287e-07f, 0.9990482330322266f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_neck":
                position = new Vector3(-0.0f, 0.1747119426727295f, 0.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_nipan_cm_L":
                position = new Vector3(-1.1102230246251565e-16f, 1.011752459589843e-09f, 0.47999998927116394f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_nipan_cm_R":
                position = new Vector3(-5.551115123125783e-17f, 1.7763568394002505e-15f, 0.47999998927116394f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_shoulder02_L":
                position = new Vector3(-0.4000000059604645f, 0.0f, 0.0f);
                rotation = new Quaternion(-3.170658544604521e-07f, 0.0034821813460439444f, -2.0861345547018573e-05f, 0.9999939799308777f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_shoulder02_R":
                position = new Vector3(0.39975666999816895f, 0.0f, 0.0f);
                rotation = new Quaternion(5.489451382345578e-07f, -0.030394624918699265f, -3.5936347558163106e-05f, 0.999538004398346f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_shoulder03_L":
                position = new Vector3(-0.20625965297222137f, -0.30000001192092896f, -1.2762414822869994e-16f);
                rotation = new Quaternion(-4.474556192235468e-07f, -0.003073355881497264f, -2.0225232219672762e-05f, 0.9999952912330627f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_shoulder03_R":
                position = new Vector3(0.20625998079776764f, -0.30000001192092896f, 0.0f);
                rotation = new Quaternion(5.207546678320796e-07f, -0.01585109904408455f, -3.547481173882261e-05f, 0.9998744130134583f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_shoulder_sk_L":
                position = new Vector3(0.05011379346251488f, -0.199950709939003f, -0.0012189142871648073f);
                rotation = new Quaternion(-2.1831583296716417e-07f, 0.031497661024332047f, -4.6923272748244926e-05f, 0.9995038509368896f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_shoulder_sk_R":
                position = new Vector3(-0.05014205351471901f, -0.1999448835849762f, -0.000698618998285383f);
                rotation = new Quaternion(2.818374014168512e-06f, -6.938893903907228e-18f, -7.215974495143301e-22f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_siri_C":
                position = new Vector3(-7.536576873690137e-08f, -0.3008999228477478f, -0.019640028476715088f);
                rotation = new Quaternion(0.0003929703962057829f, -5.731057121927119e-12f, -1.458394027764598e-08f, 0.9999999403953552f);
                scale = new Vector3(1.0f, 1.0700900554656982f, 1.070044994354248f); return true;
            case "cf_d_siri_L":
                position = new Vector3(-0.5308999419212341f, -0.3008999228477478f, -0.09910006821155548f);
                rotation = new Quaternion(0.0003936436551157385f, -2.4659485120537283e-07f, -5.77266678902788e-10f, 0.9999999403953552f);
                scale = new Vector3(1.0004500150680542f, 1.0004500150680542f, 1.0f); return true;
            case "cf_d_siri_R":
                position = new Vector3(0.5308999419212341f, -0.3008999228477478f, -0.09910007566213608f);
                rotation = new Quaternion(0.000392969639506191f, -6.0148669898951734e-12f, -1.5306186540442468e-08f, 0.9999999403953552f);
                scale = new Vector3(1.0004500150680542f, 1.0004500150680542f, 1.0f); return true;
            case "cf_d_sk_00_01":
                position = new Vector3(1.3234889800848443e-23f, 0.0f, -3.469446951953614e-18f);
                rotation = new Quaternion(-0.007853297516703606f, -5.856196638731603e-10f, -3.196663556082058e-08f, 0.9999691843986511f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_sk_01_01":
                position = new Vector3(-1.1102230246251565e-16f, 0.0f, 5.551115123125783e-17f);
                rotation = new Quaternion(-0.007853299379348755f, -1.2020408279855843e-10f, -4.5920923952280646e-08f, 0.9999691843986511f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_sk_02_01":
                position = new Vector3(-0.0f, 1.7763568394002505e-15f, 0.0f);
                rotation = new Quaternion(-0.003926675301045179f, -6.168020718178013e-06f, 0.0015706632984802127f, 0.9999910593032837f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_sk_03_01":
                position = new Vector3(-1.1102230246251565e-16f, 0.0f, 2.220446049250313e-16f);
                rotation = new Quaternion(-2.2079075131763363e-17f, -4.808311526716125e-10f, -4.591856495039792e-08f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_sk_04_01":
                position = new Vector3(-0.0f, 5.329070518200751e-15f, 0.0f);
                rotation = new Quaternion(0.0f, -0.0f, -3.19610506949175e-08f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_sk_05_01":
                position = new Vector3(1.1102230246251565e-16f, 0.0f, 2.220446049250313e-16f);
                rotation = new Quaternion(-6.770542590122833e-19f, -2.1183730117280142e-11f, -3.19610506949175e-08f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_sk_06_01":
                position = new Vector3(-0.0f, 0.0f, 1.1102230246251565e-16f);
                rotation = new Quaternion(-0.00392667343840003f, 6.167524134070845e-06f, -0.0015706653939560056f, 0.9999910593032837f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_sk_07_01":
                position = new Vector3(1.1102230246251565e-16f, 0.0f, 5.551115123125783e-17f);
                rotation = new Quaternion(-0.007853296585381031f, 2.298165130421026e-10f, -3.19602300180577e-08f, 0.9999691843986511f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_thigh00_L":
                position = new Vector3(-1.3222242745136725e-11f, 0.0f, 0.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_thigh00_R":
                position = new Vector3(-1.1870129412727692e-07f, 0.0f, 0.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_thigh01_L":
                position = new Vector3(-0.0f, -0.29972919821739197f, 0.0f);
                rotation = new Quaternion(0.0f, 6.358554401231231e-06f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_thigh01_R":
                position = new Vector3(-0.0f, -0.29972922801971436f, 0.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_thigh02_L":
                position = new Vector3(-0.0f, -2.0f, 0.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_thigh02_R":
                position = new Vector3(-0.0f, -2.0f, 0.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_thigh03_L":
                position = new Vector3(-7.391376755094825e-08f, -3.7760000228881836f, 0.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_thigh03_R":
                position = new Vector3(-9.66901891885641e-10f, -3.776031017303467f, 0.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_d_toes_L":
                position = new Vector3(3.6752632187608825e-12f, 0.0f, 0.9000003337860107f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(0.9999996423721313f, 0.9999996423721313f, 0.9999996423721313f); return true;
            case "cf_d_toes_R":
                position = new Vector3(-0.0f, 0.0f, 0.9000001549720764f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_dd_bust02_L":
                position = new Vector3(-2.7755575615628914e-17f, 8.881784197001252e-16f, -2.220446049250313e-16f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_dd_bust02_R":
                position = new Vector3(5.551115123125783e-17f, 0.0f, -2.0816681711721685e-17f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_dd_leg03_L":
                position = new Vector3(2.3522133574260806e-08f, 0.0f, -3.598591501940973e-05f);
                rotation = new Quaternion(-3.236997508793138e-05f, 3.3088134554319637e-13f, -1.0221858914860604e-08f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_dd_leg03_R":
                position = new Vector3(-3.5027575506774156e-08f, 0.0f, 0.0001830659166444093f);
                rotation = new Quaternion(0.0001488349516876042f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(0.9999995231628418f, 0.9999991655349731f, 0.9999983310699463f); return true;
            case "cf_dd_thigh02_L":
                position = new Vector3(-0.0f, 0.0f, 0.0004499653587117791f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_dd_thigh02_R":
                position = new Vector3(-0.0f, 0.0f, 0.0004499655624385923f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_j_arm00_L":
                position = new Vector3(-0.0f, 0.0f, 0.0f);
                rotation = new Quaternion(1.3941596535005374e-06f, -0.028793951496481895f, -4.1982524123795883e-08f, 0.9995853900909424f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_j_arm00_R":
                position = new Vector3(-0.0f, 0.0f, 0.0f);
                rotation = new Quaternion(-1.357103064947296e-05f, 0.02879342995584011f, 3.908872088231874e-07f, 0.9995853900909424f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_j_foot_L":
                position = new Vector3(-4.9960036108132044e-14f, -0.44999998807907104f, 0.0f);
                rotation = new Quaternion(-1.264274146706157e-06f, -1.0555678001870992e-14f, -1.3581118753203374e-15f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_j_foot_R":
                position = new Vector3(-0.0f, -0.44999998807907104f, 0.0f);
                rotation = new Quaternion(-2.5178104579026694e-07f, 6.855551063396742e-29f, 1.122190778956002e-29f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_j_forearm01_L":
                position = new Vector3(-2.174999952316284f, 0.0f, 0.0f);
                rotation = new Quaternion(0.0f, 0.06104836240410805f, -0.0f, 0.9981348514556885f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_j_forearm01_R":
                position = new Vector3(2.174999952316284f, 0.0f, 0.0f);
                rotation = new Quaternion(-3.5229624018029426e-07f, -0.06104746460914612f, -1.6791894950074493e-06f, 0.9981349110603333f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_j_hand_L":
                position = new Vector3(-2.0250000953674316f, 0.0f, 0.0f);
                rotation = new Quaternion(-1.3941812539997045e-06f, -0.03228280320763588f, -4.5031548268070765e-08f, 0.9994787573814392f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_j_hand_R":
                position = new Vector3(2.0250000953674316f, 0.0f, 0.0f);
                rotation = new Quaternion(1.3922353900852613e-05f, 0.032282426953315735f, 4.496829717481887e-07f, 0.999478816986084f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_j_head":
                position = new Vector3(-0.0f, 0.699999988079071f, 0.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_j_hips":
                position = new Vector3(-0.0f, 0.0f, 0.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_j_index01_L":
                position = new Vector3(-0.4376041889190674f, -0.032535355538129807f, 0.18027518689632416f);
                rotation = new Quaternion(0.04609920084476471f, -0.10325762629508972f, 0.9933542013168335f, -0.021450864151120186f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_j_index01_R":
                position = new Vector3(0.43763625621795654f, -0.03252430632710457f, 0.1802968978881836f);
                rotation = new Quaternion(0.021450864151120186f, 0.9933542013168335f, 0.10325762629508972f, 0.04609920084476471f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_j_index02_L":
                position = new Vector3(0.25559964776039124f, 0.0f, 0.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_j_index02_R":
                position = new Vector3(-0.2555326819419861f, 0.0f, 0.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_j_index03_L":
                position = new Vector3(0.1935001164674759f, 0.0f, 0.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_j_index03_R":
                position = new Vector3(-0.1937297284603119f, 0.0f, 0.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_j_leg01_L":
                position = new Vector3(-0.0f, -4.513000011444092f, 0.0f);
                rotation = new Quaternion(0.04361816495656967f, 8.135463502867424e-08f, -6.919252086845518e-07f, 0.9990482926368713f);
                scale = new Vector3(0.9999996423721313f, 0.9999988675117493f, 0.9999988675117493f); return true;
            case "cf_j_leg01_R":
                position = new Vector3(-5.526194399863016e-07f, -4.513000011444092f, 0.0f);
                rotation = new Quaternion(0.04361817240715027f, -2.807800658644055e-09f, -9.975605053114123e-08f, 0.9990482926368713f);
                scale = new Vector3(0.999996542930603f, 0.9999966025352478f, 0.9999966025352478f); return true;
            case "cf_j_leg03_L":
                position = new Vector3(-3.206320897675141e-08f, -4.4029998779296875f, 0.0f);
                rotation = new Quaternion(-0.03577161952853203f, 8.42049212224083e-06f, 1.207865238939121e-06f, 0.9993600249290466f);
                scale = new Vector3(0.9999982118606567f, 0.9999982118606567f, 0.9999982118606567f); return true;
            case "cf_j_leg03_R":
                position = new Vector3(-0.0f, -4.4029998779296875f, 0.0f);
                rotation = new Quaternion(-0.03577161952853203f, 1.618039249251524e-07f, -4.3979727593068674e-07f, 0.9993600249290466f);
                scale = new Vector3(0.9999998807907104f, 0.9999998807907104f, 0.9999998807907104f); return true;
            case "cf_j_little01_L":
                position = new Vector3(-0.3709583282470703f, -0.06970339268445969f, -0.22041279077529907f);
                rotation = new Quaternion(-0.154912069439888f, 0.1374596655368805f, 0.9780762791633606f, 0.021771471947431564f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_j_little01_R":
                position = new Vector3(0.37098777294158936f, -0.06969281286001205f, -0.2203199863433838f);
                rotation = new Quaternion(-0.021771471947431564f, 0.9780762791633606f, -0.1374596655368805f, -0.154912069439888f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_j_little02_L":
                position = new Vector3(0.22409968078136444f, 0.0f, 0.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_j_little02_R":
                position = new Vector3(-0.22414478659629822f, 0.0f, 0.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_j_little03_L":
                position = new Vector3(0.16829997301101685f, 0.0f, 0.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_j_little03_R":
                position = new Vector3(-0.16829997301101685f, 0.0f, 0.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_j_middle01_L":
                position = new Vector3(-0.46777015924453735f, 0.011066080071032047f, 0.022145383059978485f);
                rotation = new Quaternion(-0.0f, 0.0f, 0.9996573328971863f, -0.026176948100328445f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_j_middle01_R":
                position = new Vector3(0.46799981594085693f, 0.0110767288133502f, 0.022166818380355835f);
                rotation = new Quaternion(0.026176948100328445f, 0.9996573328971863f, 6.28142362152599e-17f, -5.960848120769839e-17f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_j_middle02_L":
                position = new Vector3(0.2907000780105591f, 0.0f, 0.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_j_middle02_R":
                position = new Vector3(-0.2909928858280182f, 0.0f, 0.0f);
                rotation = new Quaternion(-1.0296136804297745e-23f, -7.420962333526404e-07f, -7.640724565090637e-30f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_j_middle03_L":
                position = new Vector3(0.20069967210292816f, 0.0f, 0.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_j_middle03_R":
                position = new Vector3(-0.20101822912693024f, 0.0f, 0.0f);
                rotation = new Quaternion(7.72614157105804e-24f, 8.745615180400651e-20f, 6.754258598045618e-43f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_j_neck":
                position = new Vector3(-0.0f, 1.350000023841858f, -0.2930709719657898f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_j_ring01_L":
                position = new Vector3(-0.43865668773651123f, -0.01498652808368206f, -0.11752668768167496f);
                rotation = new Quaternion(-0.06974851340055466f, 0.06838475167751312f, 0.99513840675354f, -0.012576384469866753f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_j_ring01_R":
                position = new Vector3(0.43868720531463623f, -0.01497765351086855f, -0.11750492453575134f);
                rotation = new Quaternion(0.012502258643507957f, 0.9950674176216125f, -0.06836149841547012f, -0.07079040259122849f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_j_ring02_L":
                position = new Vector3(0.26190024614334106f, 0.0f, 0.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_j_ring02_R":
                position = new Vector3(-0.2616669833660126f, 0.0f, 0.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_j_ring03_L":
                position = new Vector3(0.19889992475509644f, 0.0f, 0.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_j_ring03_R":
                position = new Vector3(-0.1984507143497467f, 0.0f, 0.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_j_shoulder02_L":
                position = new Vector3(-0.10000000149011612f, -1.7763568394002505e-15f, 0.01252017542719841f);
                rotation = new Quaternion(-7.941116265150353e-23f, -0.0067701181396842f, -3.7526118831010535e-05f, 0.9999771118164062f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_j_shoulder02_R":
                position = new Vector3(0.10000000149011612f, 0.0f, 0.012519949115812778f);
                rotation = new Quaternion(1.0339994315184412e-25f, 0.006769837811589241f, 2.3599942267082952e-07f, 0.9999771118164062f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_j_shoulder_L":
                position = new Vector3(-0.10199999809265137f, 1.0080000162124634f, -0.2150000035762787f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_j_shoulder_R":
                position = new Vector3(0.10199999809265137f, 1.0080000162124634f, -0.2150000035762787f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_j_spine01":
                position = new Vector3(-0.0f, 0.10000000149011612f, 0.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_j_spine02":
                position = new Vector3(-0.0f, 0.75f, 0.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_j_spine03":
                position = new Vector3(-0.0f, 0.885362446308136f, -0.16547933220863342f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_j_thigh00_L":
                position = new Vector3(-0.0f, 0.0f, 0.0f);
                rotation = new Quaternion(-0.007853296585381031f, -8.011652425921056e-06f, 6.022239062986046e-08f, 0.9999691843986511f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_j_thigh00_R":
                position = new Vector3(-0.0f, 1.7763568394002505e-15f, -5.551115123125783e-17f);
                rotation = new Quaternion(-0.007853299379348755f, -4.448802144452202e-07f, 1.0553895535281299e-08f, 0.9999691843986511f);
                scale = new Vector3(1.0000035762786865f, 1.0000035762786865f, 1.0000035762786865f); return true;
            case "cf_j_thumb01_L":
                position = new Vector3(0.08369984477758408f, -0.09429599344730377f, 0.08982311934232712f);
                rotation = new Quaternion(-0.29188865423202515f, 0.7006842494010925f, -0.6469040513038635f, -0.07319672405719757f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_j_thumb01_R":
                position = new Vector3(-0.08369982987642288f, -0.09409543126821518f, 0.09000000357627869f);
                rotation = new Quaternion(-0.07319672405719757f, 0.6469040513038635f, 0.7006842494010925f, 0.29188865423202515f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_j_thumb02_L":
                position = new Vector3(0.380700021982193f, 0.0f, 0.0f);
                rotation = new Quaternion(0.06970269978046417f, 0.002738626440986991f, 0.039164185523986816f, 0.9967949986457825f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_j_thumb02_R":
                position = new Vector3(-0.3809693455696106f, 0.0f, 0.0f);
                rotation = new Quaternion(0.06970269978046417f, 0.002738626440986991f, 0.039164185523986816f, 0.9967949986457825f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_j_thumb03_L":
                position = new Vector3(0.21329982578754425f, 0.0f, 0.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_j_thumb03_R":
                position = new Vector3(-0.21343493461608887f, 0.0f, 0.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_j_toes_L":
                position = new Vector3(-0.0f, 2.8814594088544254e-07f, 1.100000023841858f);
                rotation = new Quaternion(1.8837198467736016e-06f, -4.6159919975252706e-07f, -9.420512583346863e-07f, 1.0f);
                scale = new Vector3(0.9999996423721313f, 0.9999996423721313f, 0.9999996423721313f); return true;
            case "cf_j_toes_R":
                position = new Vector3(-0.0f, 3.2030602596933022e-06f, 1.100000023841858f);
                rotation = new Quaternion(8.654357088744291e-07f, 2.6446920742273505e-07f, 5.263187290438509e-07f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_j_waist01":
                position = new Vector3(-0.0f, -0.10000000149011612f, 0.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_j_waist02":
                position = new Vector3(-0.0f, -0.5215109586715698f, -0.4000000059604645f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_y_arm01_L":
                position = new Vector3(-0.0f, 0.0f, 0.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_y_arm01_R":
                position = new Vector3(3.885780586188048e-15f, -1.7763568394002505e-15f, -5.551115123125783e-17f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_y_arm01_tip_L":
                position = new Vector3(-0.0f, 0.0f, 2.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_y_arm01_tip_R":
                position = new Vector3(-0.0f, 0.0f, 2.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_y_arm02_L":
                position = new Vector3(-0.0f, 0.0f, 0.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_y_arm02_R":
                position = new Vector3(-0.0f, 1.7763568394002505e-15f, 5.551115123125783e-17f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_y_arm02_tip_L":
                position = new Vector3(-0.0f, 0.0f, 2.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_y_arm02_tip_R":
                position = new Vector3(-0.0f, 0.0f, 2.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_y_arm03_L":
                position = new Vector3(-0.0f, 0.0f, 0.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_y_arm03_R":
                position = new Vector3(-0.0f, 0.0f, 0.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_y_arm03_tip_L":
                position = new Vector3(-0.0f, 0.0f, 2.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_y_arm03_tip_R":
                position = new Vector3(-0.0f, 0.0f, 2.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_y_bust01_tip_L":
                position = new Vector3(0.0f, 0.0f, 0.20000000298023224f);
                rotation = new Quaternion(0.0f, 0.0f, 0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_y_bust01_tip_R":
                position = new Vector3(0.0f, 0.0f, 0.20000000298023224f);
                rotation = new Quaternion(0.0f, 0.0f, 0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_y_bust02_tip_L":
                position = new Vector3(0.0f, 0.0f, 0.4000000059604645f);
                rotation = new Quaternion(0.0f, 0.0f, 0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_y_bust02_tip_R":
                position = new Vector3(0.0f, 0.0f, 0.4000000059604645f);
                rotation = new Quaternion(0.0f, 0.0f, 0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_y_spine01":
                position = new Vector3(-0.0f, 0.0f, 0.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_y_spine01_tip":
                position = new Vector3(-0.0f, 0.0f, 2.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_y_thigh01_L":
                position = new Vector3(-0.0f, 0.0f, 0.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_y_thigh01_R":
                position = new Vector3(-0.0f, 0.0f, 0.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_y_thigh01_tip_L":
                position = new Vector3(-0.0f, 0.0f, 2.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_y_thigh01_tip_R":
                position = new Vector3(-0.0f, 0.0f, 2.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_y_thigh02_L":
                position = new Vector3(-0.0f, 0.0f, 0.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_y_thigh02_R":
                position = new Vector3(-0.0f, 0.0f, 0.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_y_thigh02_tip_L":
                position = new Vector3(-0.0f, 0.0f, 2.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_y_thigh02_tip_R":
                position = new Vector3(-0.0f, 0.0f, 2.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_y_thigh03_L":
                position = new Vector3(-0.0f, 0.0f, 0.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_y_thigh03_R":
                position = new Vector3(-0.0f, 0.0f, 0.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_y_thigh03_tip_L":
                position = new Vector3(-0.0f, 0.0f, 2.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_y_thigh03_tip_R":
                position = new Vector3(-0.0f, 0.0f, 2.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_y_waist01":
                position = new Vector3(-1.3528318731914624e-07f, 0.0f, 0.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            case "cf_y_waist01_tip":
                position = new Vector3(-0.0f, 0.0f, 2.0f);
                rotation = new Quaternion(0.0f, -0.0f, -0.0f, 1.0f);
                scale = new Vector3(1.0f, 1.0f, 1.0f); return true;
            default:
                position = Vector3.zero; rotation = Quaternion.identity; scale = Vector3.one; return false;
        }
    }
}
