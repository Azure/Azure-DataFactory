import Template from './Template.ts';
import { ErrorCode, ValidateResult } from './types.ts';
import { formatResults } from './format.ts';
import Result from './Result.ts';

const validate = (path: string): ValidateResult => {
	const entries = Array.from(Deno.readDirSync(path));
	const results: Result[] = [];
	const templates: Template[] = entries.reduce(
		(previousValue, currentValue) => {
			try {
				return previousValue.concat(
					new Template(
						currentValue.name,
						`${path}/${currentValue.name}`,
					),
				);
			} catch (e) {
				const result = new Result(currentValue.name);
				result.errors.push({
					code: ErrorCode.PARSE_JSON_FAIL,
					detail: e.emssage,
				});
				results.push(result);
				return previousValue;
			}
		},
		[] as Template[],
	);
	results.push(...templates.map((template) => template.validate()));
	return formatResults(results);
};

export default validate;
