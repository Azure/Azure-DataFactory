import Template from './Template.ts';
import { ValidateResult } from './types.ts';
import { formatResults } from './format.ts';

const validate = (path: string): ValidateResult => {
	const entries = Array.from(Deno.readDirSync(path));
	const templates: Template[] = entries.map((entry) =>
		new Template(entry.name, `${path}/${entry.name}`)
	);
	const results = templates.map((template) => template.validate());
	return formatResults(results);
};

export default validate;
