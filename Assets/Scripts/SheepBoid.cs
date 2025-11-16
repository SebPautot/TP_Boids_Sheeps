using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Implementation of http://www.csc.kth.se/utbildning/kth/kurser/DD143X/dkand13/Group9Petter/report/Martin.Barksten.David.Rydberg.report.pdf
/// </summary>
public class SheepBoid : MonoBehaviour
{
    /// <summary>
    /// Vector created by applying all the herding rules
    /// </summary>
    Vector3 targetVelocity;

    /// <summary>
    /// Actual velocity of the sheep
    /// </summary>
    Vector3 velocity;

    public SheepHerd herd;

    /// <summary>
    /// 3.1
    /// Sigmoid function, used for impact of second multiplier
    /// </summary>
    /// <param name="x">Distance to the predator</param>
    /// <returns>Weight of the rule</returns>
    float P(float x)
    {
        return (1 / Mathf.PI) * Mathf.Atan(x / 0.3f) + 0.5f;
    }

    /// <summary>
    /// 3.2
    /// Combine the two weights affecting the rules
    /// </summary>
    /// <param name="mult1">first multiplier</param>
    /// <param name="mult2">second multipler</param>
    /// <param name="x">distance to the predator</param>
    /// <returns>Combined weights</returns>
    float CombineWeight(float mult1, float mult2, float x)
    {
        return mult1 + mult1 * P(x) * mult2;
    }

    /// <summary>
    /// 3.3
    /// In two of the rules, Separation and Escape, nearby objects are prioritized higher than
    ///those further away. This prioritization is described by an inverse square function
    /// </summary>
    /// <param name="x">Distance to the predator</param>
    /// <param name="s">Softness factor</param>
    /// <returns></returns>
    float Inv(float x, float s)
    {
        if (x == 0 || s == 0) return 0;

        return Mathf.Pow((x / s) + Mathf.Epsilon, -2.0f);
    }

    /// <summary>
    /// 3.4
    /// The Cohesion rule is calculated for each sheep s with position sp. The Cohesion vector
    ///coh(s) is directed towards the average position Sp.The rule vector is calculated
    ///with the function
    ///coh(s) = Sp − sp/|Sp − sp|
    /// </summary>
    /// <returns>coh(s) the cohesion vector</returns>
    Vector3 RuleCohesion()
    {
        return (herd.averageHerd - transform.position).normalized;
    }

    /// <summary>
    /// 3.5
    /// The Separation rule is calculated for each sheep s with position sp. The contribution
    ///of each nearby sheep si
    ///is determined by the inverse square function of the distance
    ///between the sheep with a softness factor of 1. This function can be seen in Formula
    ///(3.3). The rule vector is directed away from the sheep and calculated with the
    ///function
    ///sep(s) = sum(n,i)(sp − sip/|sp − sip| * inv(|sp − sip|, 1))
    /// </summary>
    /// <returns>sep(s) the separation vector</returns>
    Vector3 RuleSeparation()
    {
        Vector3 v = Vector3.zero;
        foreach (SheepBoid sheepBoid in herd.sheeps)
        {
            if (sheepBoid == this) continue;

            var v1 = sheepBoid.transform.position - transform.position;
            var v2 = v1.normalized * Inv(v1.magnitude, 1.0f);

            if (v2.x == float.NaN || v2.y == float.NaN || v2.z == float.NaN)
                continue;

            v += v2;
        }
        return v;
    }

    /// <summary>
    /// 3.6
    /// The Alignment rule is calculated for each sheep s. Each sheep si within a radius of
    ///50 pixels has a velocity siv that contributes equally to the final rule vector.The size
    ///of the rule vector is determined by the velocity of all nearby sheep N.The vector is
    ///directed in the average direction of the nearby sheep.The rule vector is calculated
    ///with the function
    ///ali(s) = sum(Siv,N)
    ///where
    ///N = {si: si ∈ S ∩ |sip − sp| ≤ 50}
    /// </summary>
    /// <returns>ali(s) the alignement vector</returns>
    Vector3 RuleAlignment()
    {
        Vector3 v = Vector3.zero;
        int i = 0;
        foreach (SheepBoid sheepBoid in herd.sheeps)
        {
            if (sheepBoid == this) continue;

            Vector3 dist_vec = sheepBoid.transform.position - transform.position;
            float dist_pow = dist_vec.x * dist_vec.x + dist_vec.y * dist_vec.y;

            if (dist_pow > 50 * 50) continue;

            v += sheepBoid.velocity;
            i += 1;
        }

        if (i == 0)
            return Vector3.zero;

        return v / i;
    }

    /// <summary>
    /// 3.8
    /// The Escape rule is calculated for each sheep s with a position sp. The size of the
    ///rule vector is determined by inverse square function(3.3) of the distance between
    ///the sheep and predator p with a softness factor of 10. The rule vector is directed
    ///away from the predator and is calculated with the function
    ///esc(s) = sp − pp / |sp − pp| * inv(|sp − pp|, 10)
    /// </summary>
    /// <returns>esc(s) the escape vector</returns>
    Vector3 RuleEscape()
    {
        return (herd.predator - transform.position).normalized * Inv(distanceToPredator, 10.0f);
    }

    /// <summary>
    /// 3.9
    /// Get the intended velocity of the sheep by applying all the herding rules
    /// </summary>
    /// <returns>The resulting vector of all the rules</returns>
    Vector3 ApplyRules()
    {
        var v = Vector3.zero;
        v += RuleCohesion() * CombineWeight(weightCohesionBase, weightCohesionFear, distanceToPredator);
        v += RuleSeparation() * CombineWeight(weightSeparation, weightSeparationFear, distanceToPredator);
        v += RuleAlignment() * CombineWeight(weightAlignement, weightAlignementFear, distanceToPredator);
        v += RuleEscape() * weightEscape;
        return v;
    }

    void FixedUpdate()
    {
        targetVelocity = ApplyRules();
        Move();
    }

    #region Move

    [SerializeField] float flightZoneRadius = 7;
    //Velocity under which the sheep do not move
    [SerializeField] float minVelocity = 0.1f;
    //Max velocity of the sheep
    [SerializeField] float maxVelocityBase = 1;
    //Max velocity of the sheep when a predator is close
    [SerializeField] float maxVelocityFear = 4;

    float distanceToPredator = 0;

    [SerializeField] float weightCohesionBase = 0.5f;
    [SerializeField] float weightCohesionFear = 5.0f;

    [SerializeField] float weightSeparation = 2f;
    [SerializeField] float weightSeparationFear = 0f;

    [SerializeField] float weightAlignement = 0.1f;
    [SerializeField] float weightAlignementFear = 1f;
    [SerializeField] float weightEscape = 6f;
    [SerializeField] bool clampToGround = true;
    /// <summary>
    /// Move the sheep based on the result of the rules
    /// </summary>
    void Move()
    {
        distanceToPredator = (transform.position - herd.predator).magnitude;

        //Clamp the velocity to a maximum that depends on the distance to the predator
        float currentMaxVelocity = Mathf.Lerp(maxVelocityBase, maxVelocityFear, 1 - (distanceToPredator / flightZoneRadius));

        targetVelocity = Vector3.ClampMagnitude(targetVelocity, currentMaxVelocity);

        //Ignore the velocity if it's too small
        if (targetVelocity.magnitude < minVelocity)
            return;

        //Draw the velocity as a blue line coming from the sheep in the scene view
        Debug.DrawRay(transform.position, targetVelocity, Color.blue);

        velocity = targetVelocity;

        //Make sure we don't move the sheep verticaly by mistake
        if(clampToGround)
            velocity.y = 0;

        //Move the sheep
        transform.Translate(velocity * Time.fixedDeltaTime, Space.World);
    }
    #endregion
}
