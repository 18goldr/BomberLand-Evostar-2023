import sys
import os


global program
print(os.getcwd())
AGENTS_PATH = os.path.join('agents','python3')



def main(tree_A, tree_B):
	global program
	tree_A = tree_A.replace(" ","")
	tree_B = tree_B.replace(" ","")
	#print(tree_A)
	#print(tree_B)
	program = ''
	try:
		merge_programs("A",tree_A)
	except Exception as err:
		print("PYTHONERROR",err)
		return
	program = ''
	try:
		merge_programs("B",tree_B)
	except Exception as err:
		print("PYTHONERROR",err)
		return	
	#print("Finished")

def merge_programs(agent,tree):
	global program
	#print(agent,tree)
	new_program = []
	#print(os.getcwd())
	agent_original_file = os.path.join(AGENTS_PATH,'agent_evolved_original.py')
	with open(agent_original_file,'r') as f:
		lines = f.readlines()
		for l in lines:
			if "#GPCODE" in l:
				depth = l.count(' ')//4
				convert_to_code(tree,depth)
				#print(program)
				new_program += [program]
			else: new_program += [l]
	agent_evolved_file = os.path.join(AGENTS_PATH,'agent_evolved_'+agent+'.py')
	with open(agent_evolved_file,"w") as f:
		f.write(''.join(new_program))

def convert_to_code(tree,depth):
	global program
	#print(tree,depth)
	tree = tree[1:-1]
	
	condition = ''
	i = 0
	for c in tree:
		if c == ',':break
		condition += c
		i += 1
	i += 1



	value = ''
	for c in tree[i:]:
		if c == ',':break
		value += c
		i += 1
	value = float(value)
	i +=1
	
	double_parameters = ['isEnemyInBlastRadius','enemiesAround']
	function_parameters = '(unit_id)' if condition not in double_parameters else '(my_agent_id,unit_id)'
	if condition == 'isInvulnerable':
		function_parameters = '(tick_number,unit_id)'
	program += depth*(' ')*4 + 'if self.' + condition + function_parameters + ' <= ' + str(value) +':\n'



	left = ''
	if tree[i] == '(':
		#nested condition
		parenthesis = 1
		left += tree[i]
		i += 1
		while parenthesis:
			if tree[i] == '(':
				parenthesis += 1
			elif tree[i] == ')':
				parenthesis -= 1
			left += tree[i]	
			i += 1
		#print(left)
		convert_to_code(left,depth+1)
	else:
		for c in tree[i:]:
			if c == ',':break
			left += c
			i += 1
		#left = parse_terminal(left)
		program += (' ')*4*(depth+1) + "action = '" + left.lower() + "'\n"
	i += 1
	
	program += depth*(' ')*4+ 'else:\n'

	right = ''
	if tree[i] == '(':
		#nested condition
		parenthesis = 1
		right += tree[i]
		i += 1
		while parenthesis:
			if tree[i] == '(':
				parenthesis += 1
			elif tree[i] == ')':
				parenthesis -= 1
			right += tree[i]	
			i += 1
		convert_to_code(right,depth+1)
	else:
		for c in tree[i:]:
			if c == ',':break
			right += c
			i += 1
		#right = parse_terminal(right)
		program += (' ')*4*(depth+1) +  "action = '" + right.lower() + "'\n"
	#print(condition,value,left,right)



if __name__ == '__main__':
	tree_A = sys.argv[1]
	tree_B = sys.argv[2]
	#print(tree_A,tree_B)
	#tree = '(isAmmoUp,1,(isAmmoUp,2,bomb,left),right)'
	#tree = '(isAmmoUp,3,(isAmmoUp,2,detonate,left),right)'
	main(tree_A,tree_B)