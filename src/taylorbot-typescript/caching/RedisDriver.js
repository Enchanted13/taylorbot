'use strict';

const { promisify } = require('util');
const redis = require('redis');

class RedisDriver {
    constructor(host, port, password) {
        this.redisClient = redis.createClient({
            host,
            port,
            password
        });

        this.get = promisify(this.redisClient.get).bind(this.redisClient);
        this.set = promisify(this.redisClient.set).bind(this.redisClient);
        this.setExpire = promisify(this.redisClient.setex).bind(this.redisClient);

        this.eval = promisify(this.redisClient.eval).bind(this.redisClient);
        this.delete = promisify(this.redisClient.del).bind(this.redisClient);
        this.exists = promisify(this.redisClient.exists).bind(this.redisClient);

        this.setAdd = promisify(this.redisClient.sadd).bind(this.redisClient);
        this.setRemove = promisify(this.redisClient.srem).bind(this.redisClient);
        this.setIsMember = promisify(this.redisClient.sismember).bind(this.redisClient);

        this.hashGet = promisify(this.redisClient.hget).bind(this.redisClient);
        this.hashSet = promisify(this.redisClient.hset).bind(this.redisClient);

        this.expire = promisify(this.redisClient.expire).bind(this.redisClient);
        this.increment = promisify(this.redisClient.incr).bind(this.redisClient);
        this.decrement = promisify(this.redisClient.decr).bind(this.redisClient);
    }

    multi() {
        const multi = this.redisClient.multi();

        multi.execute = promisify(multi.exec).bind(multi);

        return multi;
    }
}

module.exports = RedisDriver;