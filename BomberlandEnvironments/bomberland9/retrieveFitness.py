def parse_logs():
	with open('agents/logs/replay.json','r') as f:
		D = eval(f.read().replace('null','None'))
		payload = D['payload']
		winner = payload['winning_agent_id']
		history = payload['history']
		#ticks = len(history) + 5
		ticks = history[-1]['tick']
		with open('log.txt','w') as f2:
			f2.write(str(winner)+' '+str(ticks))

if __name__ == '__main__':
	parse_logs()