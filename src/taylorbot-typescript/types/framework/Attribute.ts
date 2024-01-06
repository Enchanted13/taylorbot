import WordArgumentType = require('../base/Word');
import { ArgumentParsingError } from '../ArgumentParsingError';
import { CommandArgumentInfo, CommandMessageContext } from '../../commands/CommandMessageContext';
import { UserAttribute } from '../../attributes/UserAttribute.js';

class AttributeArgumentType extends WordArgumentType {
    get id(): string {
        return 'attribute';
    }

    parse(val: string, { client }: CommandMessageContext, arg: CommandArgumentInfo): UserAttribute {
        const attribute = client.master.registry.attributes.resolve(val);

        if (!attribute)
            throw new ArgumentParsingError(`Attribute '${val}' doesn't exist.`);

        return attribute;
    }
}

export = AttributeArgumentType;
