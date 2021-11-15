import validate from './validate.ts';

const DEFAULT_TEMPLATE_DIRECTORY = 'templates';

const main = () => {
	const PROJECT_ROOT = Deno.args[0];
	const TEMPLATE_DIRECTORY = Deno.args[1];
	const TEMPLATES_PATH = `${PROJECT_ROOT}/${TEMPLATE_DIRECTORY ?? DEFAULT_TEMPLATE_DIRECTORY}`;
	const result = validate(TEMPLATES_PATH);
	Deno.writeAllSync(
		Deno.stdout,
		new TextEncoder().encode(JSON.stringify(result)),
	);
};

main();
