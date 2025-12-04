import { writeAllSync } from 'https://deno.land/std@0.146.0/streams/mod.ts';
import { resolve, normalize } from 'https://deno.land/std@0.146.0/path/mod.ts';
import validate from './validate.ts';

const DEFAULT_TEMPLATE_DIRECTORY = 'templates';

/**
 * Validates that a path component is safe and does not contain:
 * - Path traversal sequences (../)
 * - Null bytes
 * - Shell metacharacters that could be used for injection
 * - Absolute paths when not expected
 */
const validatePathComponent = (input: string | undefined, name: string, required: boolean): string | undefined => {
	if (input === undefined || input === '') {
		if (required) {
			throw new Error(`${name} is required but was not provided`);
		}
		return undefined;
	}

	// Check for null bytes (can be used to bypass security checks)
	if (input.includes('\0')) {
		throw new Error(`${name} contains invalid null bytes`);
	}

	// Check for path traversal attempts
	const normalized = normalize(input);
	if (normalized.includes('..') || input.includes('..')) {
		throw new Error(`${name} contains path traversal sequences (..)`);
	}

	// Check for dangerous shell metacharacters
	const dangerousChars = /[;&|`$(){}[\]<>!#*?~\n\r]/;
	if (dangerousChars.test(input)) {
		throw new Error(`${name} contains potentially dangerous characters`);
	}

	// Check for excessively long paths (DoS prevention)
	const MAX_PATH_LENGTH = 4096;
	if (input.length > MAX_PATH_LENGTH) {
		throw new Error(`${name} exceeds maximum allowed length of ${MAX_PATH_LENGTH} characters`);
	}

	return input;
};

/**
 * Validates that the resolved path is within the expected base directory
 */
const validatePathWithinBase = (basePath: string, targetPath: string): void => {
	const resolvedBase = resolve(basePath);
	const resolvedTarget = resolve(targetPath);

	if (!resolvedTarget.startsWith(resolvedBase)) {
		throw new Error(`Target path escapes the project root directory`);
	}
};

/**
 * Validates that the path exists and is a directory
 */
const validateDirectoryExists = (path: string): void => {
	try {
		const stat = Deno.statSync(path);
		if (!stat.isDirectory) {
			throw new Error(`Path exists but is not a directory: ${path}`);
		}
	} catch (error) {
		if (error instanceof Deno.errors.NotFound) {
			throw new Error(`Directory does not exist: ${path}`);
		}
		throw error;
	}
};

const outputError = (message: string): void => {
	const result = { status: 'error', detail: message };
	writeAllSync(
		Deno.stdout,
		new TextEncoder().encode(JSON.stringify(result)),
	);
};

const main = () => {
	try {
		// Validate PROJECT_ROOT
		const PROJECT_ROOT = validatePathComponent(Deno.args[0], 'Project root path', true);
		if (!PROJECT_ROOT) {
			throw new Error('Project root path is required');
		}

		// Validate TEMPLATE_DIRECTORY (optional)
		const TEMPLATE_DIRECTORY = validatePathComponent(Deno.args[1], 'Template directory', false)
			?? DEFAULT_TEMPLATE_DIRECTORY;

		// Validate the template directory name itself
		validatePathComponent(TEMPLATE_DIRECTORY, 'Template directory', false);

		// Construct and validate the full templates path
		const TEMPLATES_PATH = `${PROJECT_ROOT}/${TEMPLATE_DIRECTORY}`;

		// Ensure the templates path stays within the project root
		validatePathWithinBase(PROJECT_ROOT, TEMPLATES_PATH);

		// Verify the directory exists
		validateDirectoryExists(TEMPLATES_PATH);

		const result = validate(TEMPLATES_PATH);
		writeAllSync(
			Deno.stdout,
			new TextEncoder().encode(JSON.stringify(result)),
		);
	} catch (error) {
		const errorMessage = error instanceof Error ? error.message : 'Unknown error occurred';
		outputError(errorMessage);
		Deno.exit(1);
	}
};

main();
