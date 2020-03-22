'use strict';

const Log = require('../../tools/Logger.js');
const Format = require('../../modules/DiscordFormatter.js');

class DailyPayoutRepository {
    constructor(db, usersDAO) {
        this._db = db;
        this._usersDAO = usersDAO;
    }

    async getCanRedeem(user) {
        try {
            return await this._db.oneOrNone(
                `SELECT last_payout_at < date_trunc('day', CURRENT_TIMESTAMP) AS can_redeem,
                    date_trunc('day', ((CURRENT_TIMESTAMP + INTERVAL '1 DAY'))) AS can_redeem_at
                FROM users.daily_payouts WHERE user_id = $[user_id];`,
                {
                    user_id: user.id
                }
            );
        }
        catch (e) {
            Log.error(`Getting can redeem payout for ${Format.user(user)}: ${e}`);
            throw e;
        }
    }

    async giveDailyPay(user, payoutCount, daysForBonus, baseBonusCount, increasingBonusMultiplier) {
        try {
            const { was_streak_added, streak_count, bonus_reward } = await this._db.one(
                `INSERT INTO users.daily_payouts (user_id)
                VALUES ($[user_id])
                ON CONFLICT (user_id) DO UPDATE SET
                    last_payout_at = (CASE
                        WHEN daily_payouts.last_payout_at < date_trunc('day', CURRENT_TIMESTAMP)
                        THEN CURRENT_TIMESTAMP
                        ELSE daily_payouts.last_payout_at
                    END),
                    streak_count = (CASE
                        WHEN daily_payouts.last_payout_at < date_trunc('day', CURRENT_TIMESTAMP)
                        THEN (CASE
                            WHEN (daily_payouts.last_payout_at > date_trunc('day', (CURRENT_TIMESTAMP - INTERVAL '1 DAY')))
                            THEN daily_payouts.streak_count + 1
                            ELSE 1
                        END)
                        ELSE daily_payouts.streak_count
                    END)
                RETURNING CURRENT_TIMESTAMP = last_payout_at AS was_streak_added, streak_count, CASE
                    WHEN streak_count % $[days_for_bonus] = 0
                    THEN ($[base_bonus] + $[bonus_multiplier] * SQRT(streak_count))::bigint
                    ELSE 0
                END AS bonus_reward;`,
                {
                    user_id: user.id,
                    days_for_bonus: daysForBonus,
                    base_bonus: baseBonusCount,
                    bonus_multiplier: increasingBonusMultiplier
                }
            );

            if (was_streak_added) {
                const [{ taypoint_count }] = await this._usersDAO.addTaypointCount(this._db, [user], (global.BigInt(payoutCount) + global.BigInt(bonus_reward)).toString());
                return {
                    taypoint_count,
                    streak_count,
                    payoutCount,
                    bonus_reward
                };
            }
            else {
                return null;
            }
        }
        catch (e) {
            Log.error(`Giving daily payout ${payoutCount} to ${Format.user(user)}: ${e}`);
            throw e;
        }
    }
}

module.exports = DailyPayoutRepository;